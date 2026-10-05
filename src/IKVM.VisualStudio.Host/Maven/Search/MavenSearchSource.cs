using System.Collections.Generic;

using IKVM.VisualStudio.Host.Maven.Contracts;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Something that searches a repository: its search service, or the index it publishes.
/// </summary>
abstract class MavenSearchSource
{

    /// <summary>
    /// What searches the repository, as it is shown.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Searches for artifacts matching some text, at most some number of them, each once.
    /// </summary>
    public abstract IReadOnlyList<MavenServiceSearchResult> Search(MavenSearchText text, int count);

}
