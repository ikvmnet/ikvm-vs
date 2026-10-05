using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

using NuGet.VisualStudio;

namespace IKVM.VisualStudio.Vsix.ProjectSystem;

/// <summary>
/// Adds a package to a project through NuGet, once the user agrees.
/// </summary>
[Export]
[AppliesTo(ProjectCapabilities.PackageReferences)]
internal sealed class IkvmPackageInstaller
{

    readonly UnconfiguredProject _project;
    readonly IProjectThreadingService _threading;

    [ImportingConstructor]
    public IkvmPackageInstaller(UnconfiguredProject project, IProjectThreadingService threading)
    {
        _project = project;
        _threading = threading;
    }

    /// <summary>
    /// Asks whether to add the latest stable version of a package, saying why it is needed, and adds it if the user
    /// agrees. Returns whether it was added, on the UI thread.
    /// </summary>
    public async Task<bool> AddPackageAsync(string packageId, string reason)
    {
        await _threading.SwitchToUIThread();

        var answer = VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, $"{reason}\n\nAdd the latest version of the {packageId} package to the project?", "IKVM Dependencies", OLEMSGICON.OLEMSGICON_QUERY, OLEMSGBUTTON.OLEMSGBUTTON_YESNO, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        if (answer != (int)VSConstants.MessageBoxResult.IDYES)
            return false;

        try
        {
            // NuGet installs into the automation object of the project
            if (_project.Services.HostObject is not IVsHierarchy hierarchy || ErrorHandler.Failed(hierarchy.GetProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ExtObject, out var extObject)) || extObject is not EnvDTE.Project dteProject)
                throw new InvalidOperationException("The project cannot take NuGet packages.");

            var componentModel = (IComponentModel)await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SComponentModel));
            var installer = componentModel.GetService<IVsPackageInstaller2>();

            await TaskScheduler.Default;
            installer.InstallLatestPackage(null, dteProject, packageId, false, false);

            await _threading.SwitchToUIThread();
            return true;
        }
        catch (Exception e)
        {
            ActivityLog.TryLogError(nameof(IkvmPackageInstaller), $"Could not add {packageId}: {e}");
            await _threading.SwitchToUIThread();
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, $"Could not add the {packageId} package: {e.Message}", "IKVM Dependencies", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return false;
        }
    }

}
