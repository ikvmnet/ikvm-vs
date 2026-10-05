using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using IKVM.VisualStudio.ProjectSystem;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Shows the add commands of the entry providers in the context menu of the IKVM Dependencies node and its target
/// framework folders, and runs them there: what they add is saved to the project straight away, used in the target
/// framework of the folder, or in all from the root.
/// </summary>
[ExportCommandGroup(IkvmDependencyCommandIds.CommandSetString)]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
internal sealed class AddIkvmDependencyCommandHandler : IAsyncCommandGroupHandler
{

    readonly IkvmDependencyService _service;

    [ImportingConstructor]
    public AddIkvmDependencyCommandHandler(IkvmDependencyService service)
    {
        _service = service;
    }

    static bool IsAddCommand(long commandId) => commandId >= IkvmDependencyCommandIds.AddDependencyFirst && commandId <= IkvmDependencyCommandIds.AddDependencyLast;

    static bool IsTarget(IImmutableSet<IProjectTree> nodes) => nodes.Count == 1 && (nodes.First().Flags.Contains(IkvmDependencyTreeFlags.Root) || nodes.First().Flags.Contains(IkvmDependencyTreeFlags.TargetFramework));

    public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
    {
        if (IsAddCommand(commandId) == false || IsTarget(nodes) == false)
            return Task.FromResult(CommandStatusResult.Unhandled);

        // the providers' commands, one per menu item; past the last, no item
        var descriptions = _service.GetAddCommandDescriptions();
        var index = (int)(commandId - IkvmDependencyCommandIds.AddDependencyFirst);
        if (index >= descriptions.Count)
            return Task.FromResult(index == 0 ? new CommandStatusResult(true, commandText, CommandStatus.Supported | CommandStatus.Invisible) : CommandStatusResult.Unhandled);

        return Task.FromResult(new CommandStatusResult(true, descriptions[index] + "...", CommandStatus.Enabled | CommandStatus.Supported));
    }

    public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
    {
        if (IsAddCommand(commandId) == false || IsTarget(nodes) == false)
            return false;

        var node = nodes.First();
        var targetFramework = node.Flags.Contains(IkvmDependencyTreeFlags.TargetFramework) ? node.Caption : null;
        var session = await _service.CreateSessionAsync(targetFramework);

        var index = (int)(commandId - IkvmDependencyCommandIds.AddDependencyFirst);
        if (index >= session.AddCommands.Count)
            return false;

        var added = await session.AddAsync(session.AddCommands[index], Application.Current.MainWindow);
        await _service.SaveAddedAsync(session, added);
        return true;
    }

}
