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

    readonly IkvmDependencyService _service;

    [ImportingConstructor]
    public ManageIkvmDependenciesCommandHandler(IkvmDependencyService service)
    {
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
        IkvmDependencySession session;
        try
        {
            session = await _service.CreateSessionAsync(targetFramework);
        }
        catch (Exception e)
        {
            ActivityLog.TryLogError(nameof(ManageIkvmDependenciesCommandHandler), $"Could not read IKVM dependencies: {e}");
            throw;
        }

        var dialog = new ManageIkvmDependenciesDialog(session);
        if (dialog.ShowModal() == true)
            await _service.SaveAsync(dialog.GetChanges());

        return true;
    }

}
