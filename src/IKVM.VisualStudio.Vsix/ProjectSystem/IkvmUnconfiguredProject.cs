using System.ComponentModel.Composition;

using IKVM.VisualStudio.Vsix.Packaging;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;

namespace IKVM.VisualStudio.Vsix.ProjectSystem
{

    [Export]
    [AppliesTo(ProjectTypeCapabilities.AppliesTo)]
    [ProjectTypeRegistration(
        projectTypeGuid: ProjectType.Ikvm,
        displayName: "#21",
        displayProjectFileExtensions: "#22",
        defaultProjectExtension: "ikvmproj",
        language: "Java",
        resourcePackageGuid: IkvmPackage.PackageGuid,
        Capabilities = ProjectTypeCapabilities.Default,
        DisableAsynchronousProjectTreeLoad = true,
        PossibleProjectExtensions = "ikvmproj",
        NewProjectRequireNewFolderVsTemplate = true,
        SupportsCodespaces = true,
        SupportsSolutionChangeWithoutReload = true)]
    internal class IkvmUnconfiguredProject
    {

        [ImportingConstructor]
        public IkvmUnconfiguredProject(UnconfiguredProject unconfiguredProject)
        {
            UnconfiguredProject = unconfiguredProject;
            ProjectHierarchies = new OrderPrecedenceImportCollection<IVsHierarchy>(projectCapabilityCheckProvider: unconfiguredProject);
        }

        internal UnconfiguredProject UnconfiguredProject { get; }

        [Import]
        internal IActiveConfiguredProjectSubscriptionService SubscriptionService { get; private set; } = null!;

        [Import]
        internal IProjectThreadingService ProjectThreadingService { get; private set; } = null!;

        [Import]
        internal ActiveConfiguredProject<ConfiguredProject> ActiveConfiguredProject { get; private set; } = null!;

        [Import]
        internal ActiveConfiguredProject<IkvmConfiguredProject> IkvmActiveConfiguredProject { get; private set; } = null!;

        [ImportMany(ExportContractNames.VsTypes.IVsProject, typeof(IVsProject))]
        internal OrderPrecedenceImportCollection<IVsHierarchy> ProjectHierarchies { get; }

        internal IVsHierarchy? ProjectHierarchy => ProjectHierarchies.FirstOrDefault()?.Value;

    }

}
