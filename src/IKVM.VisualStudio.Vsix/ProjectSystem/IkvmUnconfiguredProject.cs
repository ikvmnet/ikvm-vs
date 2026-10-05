using System.ComponentModel.Composition;

using IKVM.VisualStudio.Vsix.Packaging;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;
using Microsoft.VisualStudio.Shell.Interop;

namespace IKVM.VisualStudio.Vsix.ProjectSystem;

/// <summary>
/// Configuration-independent state of an IKVM project. Also carries the registration of the IKVM project type with
/// Visual Studio.
/// </summary>
[Export]
[AppliesTo(IkvmProjectCapabilities.AppliesTo)]
[ProjectTypeRegistration(
    projectTypeGuid: ProjectType.Ikvm,
    displayName: "#21",
    displayProjectFileExtensions: "#22",
    defaultProjectExtension: "ikvmproj",
    language: "Java",
    resourcePackageGuid: IkvmPackage.PackageGuid,
    Capabilities = IkvmProjectCapabilities.Default,
    DisableAsynchronousProjectTreeLoad = true,
    PossibleProjectExtensions = "ikvmproj",
    NewProjectRequireNewFolderVsTemplate = true,
    SupportsSolutionChangeWithoutReload = true)]
internal class IkvmUnconfiguredProject
{

    /// <summary>
    /// Creates the instance for the given project.
    /// </summary>
    [ImportingConstructor]
    public IkvmUnconfiguredProject(UnconfiguredProject unconfiguredProject)
    {
        UnconfiguredProject = unconfiguredProject;
        ProjectHierarchies = new OrderPrecedenceImportCollection<IVsHierarchy>(projectCapabilityCheckProvider: unconfiguredProject);
    }

    /// <summary>
    /// The CPS unconfigured project this instance belongs to.
    /// </summary>
    internal UnconfiguredProject UnconfiguredProject { get; }

    /// <summary>
    /// Provides data sources that follow whichever configuration is active.
    /// </summary>
    [Import]
    internal IActiveConfiguredProjectSubscriptionService SubscriptionService { get; private set; } = null!;

    /// <summary>
    /// Threading service of the project, used to switch to the UI thread and join project work.
    /// </summary>
    [Import]
    internal IProjectThreadingService ProjectThreadingService { get; private set; } = null!;

    /// <summary>
    /// The currently active configured project.
    /// </summary>
    [Import]
    internal ActiveConfiguredProject<ConfiguredProject> ActiveConfiguredProject { get; private set; } = null!;

    /// <summary>
    /// The IKVM state of the currently active configured project.
    /// </summary>
    [Import]
    internal ActiveConfiguredProject<IkvmConfiguredProject> IkvmActiveConfiguredProject { get; private set; } = null!;

    /// <summary>
    /// The Visual Studio hierarchies exported for this project, in precedence order.
    /// </summary>
    [ImportMany(ExportContractNames.VsTypes.IVsProject, typeof(IVsProject))]
    internal OrderPrecedenceImportCollection<IVsHierarchy> ProjectHierarchies { get; }

    /// <summary>
    /// The Visual Studio hierarchy of this project, or <see langword="null"/> if none has been exported.
    /// </summary>
    internal IVsHierarchy? ProjectHierarchy => ProjectHierarchies.FirstOrDefault()?.Value;

}
