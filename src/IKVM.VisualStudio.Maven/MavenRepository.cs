namespace IKVM.VisualStudio.Maven;

/// <summary>
/// A Maven repository of a project.
/// </summary>
/// <param name="Id">ID of the repository, as the project names it.</param>
/// <param name="Url">URL of the repository.</param>
sealed record MavenRepository(string Id, string Url)
{

    /// <summary>
    /// Shows the repository as its ID followed by its URL.
    /// </summary>
    public override string ToString() => $"{Id} ({Url})";

}
