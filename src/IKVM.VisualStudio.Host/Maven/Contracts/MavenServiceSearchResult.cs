namespace IKVM.VisualStudio.Host.Maven.Contracts;

/// <summary>
/// An artifact found by a search.
/// </summary>
public sealed class MavenServiceSearchResult
{

    public string GroupId { get; set; } = "";

    public string ArtifactId { get; set; } = "";

    /// <summary>
    /// The newest version the search knows of.
    /// </summary>
    public string LatestVersion { get; set; } = "";

}
