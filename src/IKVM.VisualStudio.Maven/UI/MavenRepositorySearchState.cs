namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// How the current search of a repository is going.
/// </summary>
enum MavenRepositorySearchState
{

    /// <summary>
    /// Nothing is being searched for.
    /// </summary>
    None,

    /// <summary>
    /// The repository is being searched.
    /// </summary>
    Searching,

    /// <summary>
    /// The search of the repository completed, finding some number of artifacts, perhaps none.
    /// </summary>
    Found,

    /// <summary>
    /// The search of the repository failed.
    /// </summary>
    Failed,

}
