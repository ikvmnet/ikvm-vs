namespace IKVM.VisualStudio.Host.Maven.Contracts;

/// <summary>
/// The search of one repository completed, or failed, and everything all the repositories have found so far.
/// </summary>
public sealed class MavenServiceSearchUpdate
{

    /// <summary>
    /// The repository whose search completed.
    /// </summary>
    public string RepositoryId { get; set; } = "";

    /// <summary>
    /// Whether the search of the repository failed.
    /// </summary>
    public bool IsFailed { get; set; }

    /// <summary>
    /// Why the search of the repository failed.
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// How many artifacts the repository found.
    /// </summary>
    public int Count { get; set; }

    /// <summary>
    /// The artifacts all the repositories have found so far, merged: each once, with the newest version found.
    /// </summary>
    public MavenServiceSearchResult[] Results { get; set; } = System.Array.Empty<MavenServiceSearchResult>();

}
