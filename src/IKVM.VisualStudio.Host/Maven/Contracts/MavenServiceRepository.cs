namespace IKVM.VisualStudio.Host.Maven.Contracts;

/// <summary>
/// A Maven repository of a project, as <c>MavenRepositories</c> names it.
/// </summary>
public sealed class MavenServiceRepository
{

    /// <summary>
    /// The ID of the repository, which the mirrors and servers of the settings refer to.
    /// </summary>
    public string Id { get; set; } = "";

    /// <summary>
    /// The URL of the repository.
    /// </summary>
    public string Url { get; set; } = "";

}
