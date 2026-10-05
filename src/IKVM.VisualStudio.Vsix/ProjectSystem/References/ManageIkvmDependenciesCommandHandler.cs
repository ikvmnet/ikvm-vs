using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.Vsix.UI;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Handles "Manage IKVM Dependencies...", from the project, the Dependencies node, the IKVM Dependencies node or one
/// of its target framework folders.
/// </summary>
[ExportCommandGroup(IkvmDependencyCommandIds.CommandSetString)]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
internal sealed class ManageIkvmDependenciesCommandHandler : IAsyncCommandGroupHandler
{

    readonly IProjectThreadingService _threading;
    readonly IkvmDependencyService _service;

    [ImportingConstructor]
    public ManageIkvmDependenciesCommandHandler(IProjectThreadingService threading, IkvmDependencyService service)
    {
        _threading = threading;
        _service = service;
    }

    public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
    {
        if (commandId != IkvmDependencyCommandIds.ManageIkvmDependencies)
            return Task.FromResult(CommandStatusResult.Unhandled);

        return Task.FromResult(new CommandStatusResult(true, commandText, CommandStatus.Enabled | CommandStatus.Supported));
    }

    public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
    {
        if (commandId != IkvmDependencyCommandIds.ManageIkvmDependencies)
            return false;

        // invoked from a target framework folder: new references default to that framework
        var targetFramework = nodes.FirstOrDefault(i => i.Flags.Contains(IkvmDependencyTreeFlags.TargetFramework))?.Caption;
        var providers = _service.GetEntryProviders();
        IReadOnlyList<IkvmDependencyElement> elements;
        try
        {
            elements = await _service.ReadAsync(providers);
        }
        catch (Exception e)
        {
            ActivityLog.TryLogError(nameof(ManageIkvmDependenciesCommandHandler), $"Could not read IKVM dependencies: {e}");
            throw;
        }
        await _threading.SwitchToUIThread();

        var dialog = new ManageIkvmDependenciesDialog(_service.Project, _service.GetConfiguredProjects(), _service.GetTargetFrameworks(), targetFramework, providers, elements);
        if (dialog.ShowModal() != true)
            return true;

        var changes = dialog.GetChanges();
        await TaskScheduler.Default;

        try
        {
            await _service.ApplyAsync(changes);
        }
        catch (Exception e)
        {
            ActivityLog.TryLogError(nameof(ManageIkvmDependenciesCommandHandler), $"Could not save IKVM dependencies: {e}");
            await _threading.SwitchToUIThread();
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, $"Could not save IKVM dependencies: {e.Message}", "Manage IKVM Dependencies", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        return true;
    }

}
