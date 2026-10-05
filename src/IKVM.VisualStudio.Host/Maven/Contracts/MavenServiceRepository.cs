namespace IKVM.VisualStudio.Host.Maven.Contracts;

/// <summary>
/// A Maven repository of a project, as <c>MavenRepositories</c> names it.
/// </summary>
public sealed class MavenServiceRepository
{

    public string Id { get; set; } = "";

    public string Url { get; set; } = "";

}
