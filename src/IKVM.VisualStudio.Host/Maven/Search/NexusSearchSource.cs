using System;
using System.Collections.Generic;
using System.Text.Json;

using IKVM.VisualStudio.Host.Maven.Contracts;

using org.eclipse.aether.repository;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Searches a repository of Nexus Repository 3 through its REST API, which, for a group, searches its members.
/// </summary>
sealed class NexusSearchSource : MavenSearchSource
{

    /// <summary>
    /// Where the path of a repository of Nexus starts, after the root of the server.
    /// </summary>
    const string RepositoryPath = "/repository/";

    /// <summary>
    /// How many pages of components to read: Nexus returns a component for each version.
    /// </summary>
    const int MaxPages = 2;

    readonly MavenHttp _http;
    readonly RemoteRepository _api;
    readonly string _repository;

    NexusSearchSource(MavenHttp http, RemoteRepository api, string repository)
    {
        _http = http;
        _api = api;
        _repository = repository;
    }

    public override string Name => "Nexus";

    /// <summary>
    /// Gets a source for a repository whose URL is that of a repository of Nexus 3, and whose server answers as one.
    /// </summary>
    public static NexusSearchSource? Probe(MavenHttp http, RemoteRepository repository)
    {
        var url = repository.getUrl();
        var index = url.IndexOf(RepositoryPath, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;

        var name = url.Substring(index + RepositoryPath.Length).Trim('/');
        if (name.Length == 0 || name.Contains('/'))
            return null;

        var api = http.At(repository, url.Substring(0, index + 1));
        if (http.GetString(api, "service/rest/v1/status") == null)
            return null;

        return new NexusSearchSource(http, api, name);
    }

    public override IReadOnlyList<MavenServiceSearchResult> Search(MavenSearchText text, int count)
    {
        var query = "service/rest/v1/search?format=maven2&repository=" + Uri.EscapeDataString(_repository);
        query += text.IsCoordinates
            ? "&maven.groupId=" + Uri.EscapeDataString(text.GroupId!) + "&maven.artifactId=" + Uri.EscapeDataString(text.ArtifactId!)
            : "&q=" + Uri.EscapeDataString(text.Text);

        var results = new MavenSearchResults();
        string? continuation = null;
        for (var page = 0; page < MaxPages && results.Count < count; page++)
        {
            var json = _http.GetString(_api, continuation == null ? query : query + "&continuationToken=" + Uri.EscapeDataString(continuation));
            if (json == null)
                break;

            using var document = JsonDocument.Parse(json);
            if (document.RootElement.TryGetProperty("items", out var items))
                foreach (var item in items.EnumerateArray())
                    results.Add(GetString(item, "group"), GetString(item, "name"), GetString(item, "version"));

            continuation = document.RootElement.TryGetProperty("continuationToken", out var token) && token.ValueKind == JsonValueKind.String ? token.GetString() : null;
            if (continuation == null)
                break;
        }

        return results.ToList(text, count);
    }

    static string GetString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? "" : "";
    }

}
