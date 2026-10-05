namespace IKVM.VisualStudio.Maven;

/// <summary>
/// An artifact found by a search, or typed as coordinates.
/// </summary>
/// <param name="GroupId">Group ID of the artifact.</param>
/// <param name="ArtifactId">Artifact ID of the artifact.</param>
/// <param name="LatestVersion">The newest version the search knows of, or empty for typed coordinates.</param>
sealed record MavenSearchResult(string GroupId, string ArtifactId, string LatestVersion)
{

    public string Coordinates => $"{GroupId}:{ArtifactId}";

}
