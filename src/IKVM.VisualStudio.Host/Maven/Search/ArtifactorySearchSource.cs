using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

using IKVM.VisualStudio.Host.Maven.Contracts;

using org.eclipse.aether.repository;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Searches a repository of Artifactory through its REST API: a virtual repository through its members, which is what
/// Artifactory searches.
/// </summary>
sealed class ArtifactorySearchSource : MavenSearchSource
{

    /// <summary>
    /// Where the path of a file starts in what Artifactory finds, before the key of its repository.
    /// </summary>
    const string StoragePath = "/api/storage/";

    readonly MavenHttp _http;
    readonly RemoteRepository _api;
    readonly IReadOnlyList<string> _repositories;

    /// <summary>
    /// Initializes a new instance that searches the given repositories of Artifactory through its API.
    /// </summary>
    /// <param name="http">The transports.</param>
    /// <param name="api">The root of the server, where its API is.</param>
    /// <param name="repositories">The keys of the repositories searched: the members of a virtual repository, or the repository itself.</param>
    ArtifactorySearchSource(MavenHttp http, RemoteRepository api, IReadOnlyList<string> repositories)
    {
        _http = http;
        _api = api;
        _repositories = repositories;
    }

    /// <inheritdoc />
    public override string Name => "Artifactory";

    /// <summary>
    /// Gets a source for a repository whose server answers as Artifactory at the parent of the URL of the repository,
    /// whose last segment is its key.
    /// </summary>
    public static ArtifactorySearchSource? Probe(MavenHttp http, RemoteRepository repository)
    {
        var url = repository.getUrl().TrimEnd('/');
        var slash = url.LastIndexOf('/');
        if (slash < 0 || url.IndexOf("://", StringComparison.Ordinal) + 2 >= slash)
            return null;

        var key = url.Substring(slash + 1);
        var api = http.At(repository, url.Substring(0, slash + 1));
        if (http.GetString(api, "api/system/ping")?.Trim() != "OK")
            return null;

        return new ArtifactorySearchSource(http, api, GetMembers(http, api, key));
    }

    /// <summary>
    /// Gets the repositories a virtual repository aggregates, or else the repository itself.
    /// </summary>
    static IReadOnlyList<string> GetMembers(MavenHttp http, RemoteRepository api, string key)
    {
        try
        {
            if (http.GetString(api, "api/repositories/" + Uri.EscapeDataString(key)) is string json)
            {
                using var document = JsonDocument.Parse(json);
                if (document.RootElement.TryGetProperty("repositories", out var members) && members.ValueKind == JsonValueKind.Array)
                    return members.EnumerateArray().Select(i => i.GetString()).OfType<string>().ToList();
            }
        }
        catch (Exception)
        {
            // the configuration of the repository may be hidden from anonymous users: search it as it is
        }

        return new[] { key };
    }

    /// <summary>
    /// Searches with GAVC searches: for coordinates, artifacts of the group whose ID contains the artifact ID; for
    /// words, artifacts whose ID contains the text, and then groups whose ID does, when that found too few.
    /// </summary>
    public override IReadOnlyList<MavenServiceSearchResult> Search(MavenSearchText text, int count)
    {
        var repos = "&repos=" + string.Join(",", _repositories.Select(Uri.EscapeDataString));

        var results = new MavenSearchResults();
        if (text.IsCoordinates)
        {
            Find(results, "g=" + Uri.EscapeDataString(text.GroupId!) + "&a=" + Uri.EscapeDataString("*" + text.ArtifactId + "*") + repos);
        }
        else
        {
            var word = Uri.EscapeDataString("*" + text.Text + "*");
            Find(results, "a=" + word + repos);
            if (results.Count < count)
                Find(results, "g=" + word + repos);
        }

        return results.ToList(text, count);
    }

    /// <summary>
    /// Adds the artifacts of the files a GAVC search finds.
    /// </summary>
    void Find(MavenSearchResults results, string query)
    {
        if (_http.GetString(_api, "api/search/gavc?" + query) is not string json)
            return;

        using var document = JsonDocument.Parse(json);
        if (document.RootElement.TryGetProperty("results", out var files) == false)
            return;

        foreach (var file in files.EnumerateArray())
            if (file.TryGetProperty("uri", out var uri) && uri.GetString() is string value)
                AddFile(results, value);
    }

    /// <summary>
    /// Adds the artifact of a file, from its path: the key of the repository, the group, the artifact, the version
    /// and the file, whose name starts with the artifact.
    /// </summary>
    static void AddFile(MavenSearchResults results, string uri)
    {
        var index = uri.IndexOf(StoragePath, StringComparison.Ordinal);
        if (index < 0)
            return;

        var segments = uri.Substring(index + StoragePath.Length).Split('/');
        if (segments.Length < 5)
            return;

        var file = segments[segments.Length - 1];
        var version = segments[segments.Length - 2];
        var artifactId = segments[segments.Length - 3];
        if (file.StartsWith(artifactId + "-", StringComparison.Ordinal) == false)
            return;

        results.Add(string.Join(".", segments, 1, segments.Length - 4), artifactId, version);
    }

}
