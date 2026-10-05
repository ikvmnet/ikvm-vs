namespace IKVM.VisualStudio.Maven;

/// <summary>
/// An artifact found by a search, or typed as coordinates.
/// </summary>
/// <param name="GroupId">Group ID of the artifact.</param>
/// <param name="ArtifactId">Artifact ID of the artifact.</param>
/// <param name="LatestVersion">The newest version the search knows of, or empty for typed coordinates.</param>
sealed record MavenSearchResult(string GroupId, string ArtifactId, string LatestVersion)
{

    /// <summary>
    /// The artifact as <c>groupId:artifactId</c>, which identifies it among the results and the references of the
    /// project.
    /// </summary>
    public string Coordinates =>$"{GroupId}:{ArtifactId}";

    /// <summary>
    /// Whether the artifact was typed as coordinates rather than found.
    /// </summary>
    public bool IsTyped { get; init; }

    /// <summary>
    /// The version the project already references the artifact at, if it does.
    /// </summary>
    public string? ProjectVersion { get; init; }

    /// <summary>
    /// Whether the project already references the artifact.
    /// </summary>
    public bool IsInProject => ProjectVersion != null;

    /// <summary>
    /// Says that the project references the artifact, and at which version.
    /// </summary>
    public string InProjectText => ProjectVersion is { Length: > 0 } version ? $"In project: {version}" : "In project";

}
