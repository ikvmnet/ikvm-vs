using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.ProjectSystem;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Describes candidate references using the project's own IKVM package, by running its
/// <c>DescribeIkvmReferenceCandidates</c> target.
/// </summary>
[Export]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
internal sealed class IkvmReferenceDescriber
{

    const string TargetName = "DescribeIkvmReferenceCandidates";
    const string CandidatesProperty = "IkvmReferenceCandidates";

    readonly UnconfiguredProject _project;

    /// <summary>
    /// Initializes a new instance for the project whose IKVM package does the describing.
    /// </summary>
    [ImportingConstructor]
    public IkvmReferenceDescriber(UnconfiguredProject project)
    {
        _project = project;
    }

    /// <summary>
    /// Describes the given full paths. Paths that cannot be described are omitted.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, IkvmReferenceDescription>> DescribeAsync(IReadOnlyCollection<string> paths, CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, IkvmReferenceDescription>(StringComparer.OrdinalIgnoreCase);
        if (paths.Count == 0)
            return result;

        var configuredProject = await _project.GetSuggestedConfiguredProjectAsync();
        var build = configuredProject?.Services.Build;
        if (build == null)
        {
            ActivityLog.TryLogWarning(nameof(IkvmReferenceDescriber), "No build service for the project.");
            return result;
        }

        try
        {
            var properties = ImmutableDictionary<string, string>.Empty.Add(CandidatesProperty, string.Join(";", paths.Select(Microsoft.Build.Evaluation.ProjectCollection.Escape)));

            // package targets are imported into the per target framework builds only, so build the configured project's
            if (configuredProject!.ProjectConfiguration.Dimensions.TryGetValue("TargetFramework", out var targetFramework) && string.IsNullOrEmpty(targetFramework) == false)
                properties = properties.SetItem("TargetFramework", targetFramework);

            var logger = new ErrorCollectingLogger();
            var buildResult = await build.BuildAsync(new[] { TargetName }, cancellationToken, true, properties, loggers: ImmutableHashSet.Create<Microsoft.Build.Framework.ILogger>(logger));
            if (logger.Errors.Count > 0)
                ActivityLog.TryLogWarning(nameof(IkvmReferenceDescriber), $"{TargetName} failed: {string.Join(" | ", logger.Errors)}");
            if (buildResult?.MSBuildResult?.ResultsByTarget is { } resultsByTarget && resultsByTarget.TryGetValue(TargetName, out var targetResult))
            {
                if (targetResult.ResultCode != Microsoft.Build.Execution.TargetResultCode.Success)
                    ActivityLog.TryLogWarning(nameof(IkvmReferenceDescriber), $"{TargetName} {targetResult.ResultCode}: {targetResult.Exception}");

                foreach (var item in targetResult.Items)
                {
                    var original = item.GetMetadata(IkvmReferenceRules.OriginalItemSpecMetadata);
                    var path = string.IsNullOrEmpty(original) ? item.ItemSpec : original;
                    result[path] = new IkvmReferenceDescription(
                        path,
                        Empty(item.GetMetadata(IkvmReferenceRules.AssemblyNameMetadata)),
                        Empty(item.GetMetadata(IkvmReferenceRules.AssemblyVersionMetadata)),
                        string.Equals(item.GetMetadata(IkvmReferenceRules.IsResolvedMetadata), "true", StringComparison.OrdinalIgnoreCase),
                        Empty(item.GetMetadata(IkvmReferenceRules.DiagnosticMetadata)));
                }
            }
        }
        catch (Exception e) when (cancellationToken.IsCancellationRequested == false)
        {
            // an older IKVM package without the target, or a failed build: no descriptions
            ActivityLog.TryLogWarning(nameof(IkvmReferenceDescriber), $"Could not describe IKVM reference candidates: {e}");
        }

        return result;
    }

    /// <summary>
    /// Treats empty metadata values, which MSBuild returns for unset metadata, as <c>null</c>.
    /// </summary>
    static string? Empty(string value) => string.IsNullOrEmpty(value) ? null : value;

}
