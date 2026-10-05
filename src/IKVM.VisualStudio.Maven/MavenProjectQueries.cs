using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace IKVM.VisualStudio.Maven;

/// <summary>
/// Asks a project's IKVM.Maven.Sdk about Maven: its repositories, and, through Maven Resolver in the build, the versions
/// of an artifact, with the settings, mirrors and credentials a build would use.
/// </summary>
static class MavenProjectQueries
{

    const string VersionsTarget = "ResolveMavenArtifactVersions";
    const string RepositoriesProperty = "MavenRepositories";

    /// <summary>
    /// The repository a project without any configured uses.
    /// </summary>
    public static MavenRepository Central { get; } = new MavenRepository("central", "https://repo1.maven.org/maven2/");

    /// <summary>
    /// Gets the repositories of a project, as <c>MavenRepositories</c> lists them: <c>id=url</c>, separated by
    /// semicolons. Without any, Maven Central.
    /// </summary>
    public static async Task<IReadOnlyList<MavenRepository>> GetRepositoriesAsync(ConfiguredProject project)
    {
        try
        {
            var properties = project.Services.ProjectPropertiesProvider!.GetCommonProperties();
            var value = await properties.GetEvaluatedPropertyValueAsync(RepositoriesProperty);

            var result = value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(i => i.Split(new[] { '=' }, 2))
                .Where(i => i.Length == 2 && i[0].Trim().Length > 0 && i[1].Trim().Length > 0)
                .Select(i => new MavenRepository(i[0].Trim(), i[1].Trim()))
                .ToList();

            return result.Count > 0 ? result : new[] { Central };
        }
        catch (Exception e)
        {
            ActivityLog.TryLogWarning(nameof(MavenProjectQueries), $"Could not read the Maven repositories: {e}");
            return new[] { Central };
        }
    }

    /// <summary>
    /// Gets the versions of an artifact in the repositories of a project, newest first, or none when the project's
    /// IKVM.Maven.Sdk cannot list them.
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetVersionsAsync(ConfiguredProject project, string groupId, string artifactId, CancellationToken cancellationToken)
    {
        var build = project.Services.Build;
        if (build == null)
            return Array.Empty<string>();

        try
        {
            var properties = ImmutableDictionary<string, string>.Empty
                .Add("MavenArtifactVersionsGroupId", groupId)
                .Add("MavenArtifactVersionsArtifactId", artifactId);

            // package targets are imported into the per target framework builds only
            if (project.ProjectConfiguration.Dimensions.TryGetValue("TargetFramework", out var targetFramework) && string.IsNullOrEmpty(targetFramework) == false)
                properties = properties.SetItem("TargetFramework", targetFramework);

            var result = await build.BuildAsync(new[] { VersionsTarget }, cancellationToken, true, properties);
            if (result?.MSBuildResult?.ResultsByTarget is { } targets && targets.TryGetValue(VersionsTarget, out var target))
                return target.Items.Select(i => i.ItemSpec).ToList();
        }
        catch (Exception e) when (cancellationToken.IsCancellationRequested == false)
        {
            // an IKVM.Maven.Sdk without the target, or a failed build: no versions to offer
            ActivityLog.TryLogWarning(nameof(MavenProjectQueries), $"Could not list the versions of {groupId}:{artifactId}: {e}");
        }

        return Array.Empty<string>();
    }

}
