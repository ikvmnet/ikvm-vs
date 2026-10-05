using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Host.Maven.Contracts;
using IKVM.VisualStudio.Host.Maven.Search;

using org.eclipse.aether.artifact;
using org.eclipse.aether.resolution;

namespace IKVM.VisualStudio.Host.Maven;

/// <summary>
/// Answers questions about Maven with Maven's own libraries: Maven Resolver, and Maven Indexer.
/// </summary>
sealed class MavenService : IMavenService
{

    public Task<string[]> GetVersionsAsync(MavenServiceRepository[] repositories, string groupId, string artifactId, CancellationToken cancellationToken)
    {
        return Task.Run(() =>
        {
            var maven = new MavenEnvironment(repositories ?? Array.Empty<MavenServiceRepository>());
            var session = maven.CreateSession();

            // every version: the range from the lowest
            var artifact = new DefaultArtifact(groupId, artifactId, "", "jar", "[0,)");
            var result = maven.RepositorySystem.resolveVersionRange(session, new VersionRangeRequest(artifact, maven.Repositories, null));

            return ((IEnumerable)result.getVersions())
                .Cast<org.eclipse.aether.version.Version>()
                .Reverse()
                .Select(i => i.toString())
                .ToArray();
        }, cancellationToken);
    }

    public Task<MavenServiceSearchStatus[]> GetSearchStatusAsync(MavenServiceRepository[] repositories, CancellationToken cancellationToken)
    {
        return MavenSearches.GetStatusAsync(new MavenEnvironment(repositories ?? Array.Empty<MavenServiceRepository>()), cancellationToken);
    }

    public IAsyncEnumerable<MavenServiceSearchResult[]> SearchAsync(MavenServiceRepository[] repositories, string text, int count, CancellationToken cancellationToken)
    {
        return MavenSearches.SearchAsync(new MavenEnvironment(repositories ?? Array.Empty<MavenServiceRepository>()), text ?? "", Math.Max(1, count), cancellationToken);
    }

}
