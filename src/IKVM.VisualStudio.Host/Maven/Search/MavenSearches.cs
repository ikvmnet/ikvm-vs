using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Host.Maven.Contracts;

using org.apache.maven.search.backend.smo.@internal;
using org.eclipse.aether.repository;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Searches repositories each the best way it allows: through its search service, which probing it finds, or else
/// through the index it publishes, kept up to date in the background. What probing finds is kept for a while.
/// </summary>
static class MavenSearches
{

    /// <summary>
    /// How long to keep what probing a repository found.
    /// </summary>
    static readonly TimeSpan ProbeLifetime = TimeSpan.FromHours(1);

    /// <summary>
    /// How long to keep what probing a repository found, when it could not be reached.
    /// </summary>
    static readonly TimeSpan FailedProbeLifetime = TimeSpan.FromMinutes(5);

    /// <summary>
    /// What the index of a repository is found by.
    /// </summary>
    const string IndexProperties = ".index/nexus-maven-repository-index.properties";

    static readonly ConcurrentDictionary<string, Task<MavenRepositorySearch>> Probes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets how each repository is searched, starting to download or update the indexes of those searched through
    /// one.
    /// </summary>
    public static async Task<MavenServiceSearchStatus[]> GetStatusAsync(MavenEnvironment maven, CancellationToken cancellationToken)
    {
        var (http, repositories, searches) = await ProbeAsync(maven, cancellationToken);

        return repositories.Zip(searches, (repository, search) =>
        {
            var status = new MavenServiceSearchStatus()
            {
                RepositoryId = repository.getId(),
                Url = repository.getUrl(),
                Method = search.Method,
                IsReady = search.Method == MavenServiceSearchMethod.Server,
                Message = search.Message,
            };

            if (search.Index is MavenIndex index)
            {
                index.Update(http, repository);
                index.GetSource();
                status.IsReady = index.IsReady;
                status.IsUpdating = index.IsUpdating;
                status.Message = index.Message;
            }

            return status;
        }).ToArray();
    }

    /// <summary>
    /// Searches each repository that can be searched, yielding what they found so far, merged, as each completes.
    /// Fails only when every search does.
    /// </summary>
    public static async IAsyncEnumerable<MavenServiceSearchResult[]> SearchAsync(MavenEnvironment maven, string text, int count, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (http, repositories, searches) = await ProbeAsync(maven, cancellationToken);
        var query = MavenSearchText.Parse(text);

        var sources = new List<MavenSearchSource>();
        for (var i = 0; i < repositories.Count; i++)
        {
            if (searches[i].Server is MavenSearchSource server)
                sources.Add(server);

            if (searches[i].Index is MavenIndex index)
            {
                index.Update(http, repositories[i]);
                if (index.GetSource() is MavenSearchSource source)
                    sources.Add(source);
            }
        }

        if (sources.Count == 0)
        {
            yield return Array.Empty<MavenServiceSearchResult>();
            yield break;
        }

        var pending = sources.Select(i => Task.Run(() => i.Search(query, count), cancellationToken)).ToList();
        var results = new MavenSearchResults();
        var completed = false;
        Exception? failure = null;

        while (pending.Count > 0)
        {
            var task = await Task.WhenAny(pending).WaitAsync(cancellationToken);
            pending.Remove(task);

            if (task.Status != TaskStatus.RanToCompletion)
            {
                failure ??= task.Exception?.InnerException;
                continue;
            }

            completed = true;
            foreach (var result in task.Result)
                results.Add(result.GroupId, result.ArtifactId, result.LatestVersion);

            yield return results.ToList(query, count).ToArray();
        }

        if (completed == false && failure != null)
            throw failure;
    }

    /// <summary>
    /// Gets the repositories as the settings reach them, and how each is searched.
    /// </summary>
    static async Task<(MavenHttp Http, IReadOnlyList<RemoteRepository> Repositories, MavenRepositorySearch[] Searches)> ProbeAsync(MavenEnvironment maven, CancellationToken cancellationToken)
    {
        var http = new MavenHttp(maven, maven.CreateSession());
        var repositories = maven.GetEffectiveRepositories(http.Session);
        var searches = await Task.WhenAll(repositories.Select(i => GetSearch(http, i))).WaitAsync(cancellationToken);
        return (http, repositories, searches);
    }

    /// <summary>
    /// Gets how a repository is searched, probing it unless it was probed recently.
    /// </summary>
    static Task<MavenRepositorySearch> GetSearch(MavenHttp http, RemoteRepository repository)
    {
        var key = repository.getId() + " " + repository.getUrl();
        while (true)
        {
            var task = Probes.GetOrAdd(key, _ => Task.Run(() => Probe(http, repository)));
            if (task.IsCompletedSuccessfully == false || task.Result.Expires > DateTime.UtcNow)
                return task;

            Probes.TryRemove(new KeyValuePair<string, Task<MavenRepositorySearch>>(key, task));
        }
    }

    /// <summary>
    /// Finds how a repository can be searched: through Nexus or Artifactory, when its server is one; through the
    /// search service of Maven Central, when it is Maven Central; or else through the index it publishes.
    /// </summary>
    static MavenRepositorySearch Probe(MavenHttp http, RemoteRepository repository)
    {
        var failed = false;

        T? Try<T>(Func<T?> probe) where T : class
        {
            try
            {
                return probe();
            }
            catch (Exception)
            {
                failed = true;
                return null;
            }
        }

        var now = DateTime.UtcNow;

        if (Try(() => NexusSearchSource.Probe(http, repository)) is MavenSearchSource nexus)
            return new MavenRepositorySearch(MavenServiceSearchMethod.Server, nexus, null, "Searched with Nexus.", now + ProbeLifetime);

        if (Try(() => ArtifactorySearchSource.Probe(http, repository)) is MavenSearchSource artifactory)
            return new MavenRepositorySearch(MavenServiceSearchMethod.Server, artifactory, null, "Searched with Artifactory.", now + ProbeLifetime);

        // the search service of Maven Central is elsewhere than the repository: the backend knows where
        if (repository.getId() == SmoSearchBackendImpl.DEFAULT_REPOSITORY_ID)
        {
            var backend = new SmoSearchBackendImpl(SmoSearchBackendImpl.DEFAULT_BACKEND_ID, SmoSearchBackendImpl.DEFAULT_REPOSITORY_ID, SmoSearchBackendImpl.DEFAULT_SMO_URI, new MavenSmoTransport(http, repository));
            return new MavenRepositorySearch(MavenServiceSearchMethod.Server, new MavenSearchApiSource("Maven Central search", backend), null, "Searched with the search service of Maven Central.", now + ProbeLifetime);
        }

        if (Try(() => http.GetString(repository, IndexProperties)) != null)
            return new MavenRepositorySearch(MavenServiceSearchMethod.Index, null, MavenIndex.Get(repository), "", now + ProbeLifetime);

        if (failed)
            return new MavenRepositorySearch(MavenServiceSearchMethod.None, null, null, "Could not be reached.", now + FailedProbeLifetime);

        return new MavenRepositorySearch(MavenServiceSearchMethod.None, null, null, "Has no search service, and publishes no index.", now + ProbeLifetime);
    }

}
