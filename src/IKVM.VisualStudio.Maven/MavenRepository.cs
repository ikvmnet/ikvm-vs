namespace IKVM.VisualStudio.Maven;

/// <summary>
/// A Maven repository of a project.
/// </summary>
sealed record MavenRepository(string Id, string Url)
{

    public override string ToString() => $"{Id} ({Url})";

}
