namespace IKVM.VisualStudio.Vsix.ProjectSystem;

static class IkvmProjectCapabilities
{

    /// <summary>
    /// Capability unique to IKVM projects. Declared by the project type registration, so it is present from the
    /// start of project load, before MSBuild evaluation contributes any capabilities.
    /// </summary>
    public const string UniqueCapability = "IKVM";

    public const string AppliesTo = UniqueCapability;

    /// <summary>
    /// Capabilities fixed for every project of the IKVM project type. Further capabilities (Java, Managed,
    /// DependenciesTree, LaunchProfiles, PackageReferences, etc) are contributed by IKVM.NET.Sdk and the managed
    /// design time targets during evaluation.
    /// </summary>
    public const string Default =
        UniqueCapability + "; " +
        AppDesigner + "; " +
        HandlesOwnReload + "; " +
        OpenProjectFile + "; " +
        PreserveFormatting + "; " +
        ProjectConfigurationsDeclaredDimensions + "; " +
        DotNet + "; " +
        UseProjectEvaluationCache;

    const string AppDesigner = nameof(AppDesigner);
    const string OpenProjectFile = nameof(OpenProjectFile);
    const string HandlesOwnReload = Microsoft.VisualStudio.ProjectSystem.ProjectCapabilities.HandlesOwnReload;
    const string PreserveFormatting = nameof(PreserveFormatting);
    const string ProjectConfigurationsDeclaredDimensions = Microsoft.VisualStudio.ProjectSystem.ProjectCapabilities.ProjectConfigurationsDeclaredDimensions;
    const string UseProjectEvaluationCache = Microsoft.VisualStudio.ProjectSystem.ProjectCapabilities.UseProjectEvaluationCache;
    const string DotNet = ".NET";

}
