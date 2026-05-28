using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem
{

    static class ProjectCapabilities
    {

        public const string UniqueCapability = "IKVM";

        public const string AppliesTo = "Java & Managed";

        public const string Default =
            UniqueCapability + "; " +
            AppDesigner + "; " +
            EditAndContinue + "; " +
            HandlesOwnReload + "; " +
            OpenProjectFile + "; " +
            PreserveFormatting + "; " +
            ProjectConfigurationsDeclaredDimensions + "; " +
            DotNet + "; " +
            SharedImports + "; " +
            UseProjectEvaluationCache;

        const string AppDesigner = nameof(AppDesigner);
        const string DependenciesTree = nameof(DependenciesTree);
        const string ProjectImportsTree = nameof(ProjectImportsTree);
        const string EditAndContinue = nameof(EditAndContinue);
        const string LaunchProfiles = nameof(LaunchProfiles);
        const string OpenProjectFile = nameof(OpenProjectFile);
        const string HandlesOwnReload = Microsoft.VisualStudio.ProjectSystem.ProjectCapabilities.HandlesOwnReload;
        const string Pack = nameof(Pack); // Keep this in sync with Microsoft.VisualStudio.Editors.ProjectCapability.Pack
        const string PackageReferences = Microsoft.VisualStudio.ProjectSystem.ProjectCapabilities.PackageReferences;
        const string PreserveFormatting = nameof(PreserveFormatting);
        const string ProjectConfigurationsDeclaredDimensions = Microsoft.VisualStudio.ProjectSystem.ProjectCapabilities.ProjectConfigurationsDeclaredDimensions;
        const string UseProjectEvaluationCache = Microsoft.VisualStudio.ProjectSystem.ProjectCapabilities.UseProjectEvaluationCache;
        const string ProjectPropertyInterception = nameof(ProjectPropertyInterception);
        const string DotNet = ".NET";
        const string SharedImports = Microsoft.VisualStudio.ProjectSystem.ProjectCapabilities.SharedImports;

    }

}
