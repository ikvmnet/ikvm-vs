namespace IKVM.VisualStudio.Host.Maven.Contracts;

/// <summary>
/// How a repository is searched, and whether it can be yet.
/// </summary>
public sealed class MavenServiceSearchStatus
{

    public string RepositoryId { get; set; } = "";

    public string Url { get; set; } = "";

    public MavenServiceSearchMethod Method { get; set; }

    /// <summary>
    /// Whether searches include the repository yet: an index is not searched until it has been downloaded.
    /// </summary>
    public bool IsReady { get; set; }

    /// <summary>
    /// Whether the index of the repository is being downloaded or updated.
    /// </summary>
    public bool IsUpdating { get; set; }

    /// <summary>
    /// What is searched, or what is happening to the index.
    /// </summary>
    public string Message { get; set; } = "";

}
