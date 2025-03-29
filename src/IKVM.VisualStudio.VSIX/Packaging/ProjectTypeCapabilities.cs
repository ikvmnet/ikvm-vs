using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.VSIX.Packaging
{

    static class ProjectTypeCapabilities
    {

        public const string Default =
            AppDesigner + "; " +
            EditAndContinue + "; " +
            HandlesOwnReload + "; " +
            OpenProjectFile + "; " +
            PreserveFormatting + "; " +
            ProjectConfigurationsDeclaredDimensions + "; " +
            LanguageService + "; " +
            DotNet + "; " +
            SharedImports + "; " +
            UseProjectEvaluationCache;

        const string AppDesigner = nameof(AppDesigner);
        const string AppSettings = nameof(AppSettings);
        const string DependenciesTree = nameof(DependenciesTree);
        const string ProjectImportsTree = nameof(ProjectImportsTree);
        const string EditAndContinue = nameof(EditAndContinue);
        const string LaunchProfiles = nameof(LaunchProfiles);
        const string OpenProjectFile = nameof(OpenProjectFile);
        const string HandlesOwnReload = ProjectCapabilities.HandlesOwnReload;
        const string Pack = nameof(Pack); // Keep this in sync with Microsoft.VisualStudio.Editors.ProjectCapability.Pack
        const string PackageReferences = ProjectCapabilities.PackageReferences;
        const string PreserveFormatting = nameof(PreserveFormatting);
        const string ProjectConfigurationsDeclaredDimensions = ProjectCapabilities.ProjectConfigurationsDeclaredDimensions;
        const string LanguageService = nameof(LanguageService);
        const string UseProjectEvaluationCache = ProjectCapabilities.UseProjectEvaluationCache;
        const string SingleTargetBuildForStartupProjects = nameof(SingleTargetBuildForStartupProjects);
        const string ProjectPropertyInterception = nameof(ProjectPropertyInterception);
        const string WindowsForms = nameof(WindowsForms);
        const string WPF = nameof(WPF);
        const string SupportUniversalAuthentication = nameof(SupportUniversalAuthentication);
        const string DiagnoseVisualStudioComponents = nameof(DiagnoseVisualStudioComponents);
        const string DotNet = ".NET";
        const string SharedImports = ProjectCapabilities.SharedImports;

    }

}
