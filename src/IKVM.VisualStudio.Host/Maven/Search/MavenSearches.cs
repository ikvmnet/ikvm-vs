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
                status.Progress = index.Progress;
                status.Message = index.Message;
            }

            return status;
        }).ToArray();
    }

    /// <summary>
    /// Searches each repository that can be searched, yielding each as it completes or fails, with what all of them
    /// found so far, merged.
    /// </summary>
    public static async IAsyncEnumerable<MavenServiceSearchUpdate> SearchAsync(MavenEnvironment maven, string text, int count, [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var (http, repositories, searches) = await ProbeAsync(maven, cancellationToken);
        var query = MavenSearchText.Parse(text);

        var sources = new List<(string RepositoryId, MavenSearchSource Source)>();
        for (var i = 0; i < repositories.Count; i++)
        {
            if (searches[i].Server is MavenSearchSource server)
                sources.Add((repositories[i].getId(), server));

            if (searches[i].Index is MavenIndex index)
            {
                index.Update(http, repositories[i]);
                if (index.GetSource() is MavenSearchSource source)
                    sources.Add((repositories[i].getId(), source));
            }
        }

        var pending = sources.ToDictionary(i => Task.Run(() => i.Source.Search(query, count), cancellationToken), i => i.RepositoryId);
        var results = new MavenSearchResults();

        while (pending.Count > 0)
        {
            var task = await Task.WhenAny(pending.Keys).WaitAsync(cancellationToken);
            var update = new MavenServiceSearchUpdate() { RepositoryId = pending[task] };
            pending.Remove(task);

            try
            {
                var found = await task;
                update.Count = found.Count;
                foreach (var result in found)
                    results.Add(result.GroupId, result.ArtifactId, result.LatestVersion);
            }
            catch (Exception e)
            {
                update.IsFailed = true;
                update.Message = e.Message;
            }

            update.Results = results.ToList(query, count).ToArray();
            yield return update;
        }
    }

    /// <summary>
    /// Gets the repositories as the settings reach them, and how each is searched.
    /// </summary>
    static async Task<(MavenHttp Http, IReadOnlyList<RemoteRepository> Repositories, MavenRepositorySearch[] Searches)> ProbeAsync(MavenEnvironment maven, CancellationToken cancellationToken)
    {
        var http = new MavenHttp(maven, maven.CreateSession());
        var repositories = maven.GetEffectiveRepositories(http.Session);
        var searches = await Task.WhenAll(repositories.Select(i => GetSearchAsync(http, i))).WaitAsync(cancellationToken);
        return (http, repositories, searches);
    }

    /// <summary>
    /// Gets how a repository is searched, probing it unless it was probed recently.
    /// </summary>
    static async Task<MavenRepositorySearch> GetSearchAsync(MavenHttp http, RemoteRepository repository)
    {
        var key = repository.getId() + " " + repository.getUrl();
        while (true)
        {
            var task = Probes.GetOrAdd(key, _ => Task.Run(() => Probe(http, repository)));
            var search = await task;
            if (search.Expires > DateTime.UtcNow)
                return search;

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
