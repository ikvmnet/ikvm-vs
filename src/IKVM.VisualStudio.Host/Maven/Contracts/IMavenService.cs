using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace IKVM.VisualStudio.Host.Maven.Contracts;

/// <summary>
/// Maven, through Maven Resolver, in a ServiceHub host of its own.
/// </summary>
public interface IMavenService
{

    /// <summary>
    /// Gets the versions of an artifact in the given repositories, newest first, with the user's Maven settings:
    /// mirrors, proxies and credentials.
    /// </summary>
    Task<string[]> GetVersionsAsync(MavenServiceRepository[] repositories, string groupId, string artifactId, CancellationToken cancellationToken);

    /// <summary>
    /// Gets how each of the given repositories, with the mirrors of the settings applied, is searched: through its
    /// search service when it has one, or else through the index it publishes, which this starts downloading or
    /// updating in the background.
    /// </summary>
    Task<MavenServiceSearchStatus[]> GetSearchStatusAsync(MavenServiceRepository[] repositories, CancellationToken cancellationToken);

    /// <summary>
    /// Searches the given repositories that can be searched for artifacts matching some text: words in their
    /// coordinates, or <c>groupId:artifactId</c>. Each time the search of a repository completes, yields everything
    /// found so far, merged, so that slow repositories do not hold up the others. Fails only when every search does.
    /// </summary>
    IAsyncEnumerable<MavenServiceSearchResult[]> SearchAsync(MavenServiceRepository[] repositories, string text, int count, CancellationToken cancellationToken);

}
