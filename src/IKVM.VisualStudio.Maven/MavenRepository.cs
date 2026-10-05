using System;

namespace IKVM.VisualStudio.Maven;

/// <summary>
/// A Maven repository of a project.
/// </summary>
sealed record MavenRepository(string Id, string Url)
{

    /// <summary>
    /// Whether the repository is Maven Central, the only one with a search service.
    /// </summary>
    public bool IsCentral => Uri.TryCreate(Url, UriKind.Absolute, out var uri) && (uri.Host is "repo1.maven.org" or "repo.maven.apache.org");

    public override string ToString() => $"{Id} ({Url})";

}
