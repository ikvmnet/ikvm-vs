using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;

namespace IKVM.VisualStudio.Maven;

/// <summary>
/// Searches Maven Central for artifacts. Maven itself has no search: this is the service Central runs.
/// </summary>
static class MavenCentralSearch
{

    const string Endpoint = "https://central.sonatype.com/solrsearch/select";

    static readonly HttpClient Client = new HttpClient() { Timeout = TimeSpan.FromSeconds(20) };

    /// <summary>
    /// Searches by text: words in the group or artifact, or <c>groupId:artifactId</c>.
    /// </summary>
    public static async Task<IReadOnlyList<MavenSearchResult>> SearchAsync(string text, CancellationToken cancellationToken)
    {
        text = text.Trim();
        if (text.Length == 0)
            return Array.Empty<MavenSearchResult>();

        // coordinates search their own fields
        var parts = text.Split(':');
        var query = parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0 ? $"g:\"{parts[0]}\" AND a:\"{parts[1]}\"" : text;

        using var response = await Client.GetAsync($"{Endpoint}?q={Uri.EscapeDataString(query)}&rows=40&wt=json", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        return (json["response"]?["docs"] as JArray ?? new JArray())
            .Select(i => new MavenSearchResult((string?)i["g"] ?? "", (string?)i["a"] ?? "", (string?)i["latestVersion"] ?? ""))
            .Where(i => i.GroupId.Length > 0 && i.ArtifactId.Length > 0)
            .ToList();
    }

}
