using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using IKVM.VisualStudio.ProjectSystem;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Handles Remove on nodes of the IKVM Dependencies tree flagged <see cref="IkvmDependencyTreeFlags.Reference"/>,
/// removing the items named by their browse objects. Items defined by MSBuild expressions or wildcards cannot be
/// removed here.
/// </summary>
[ExportCommandGroup(VSConstants.CMDSETID.StandardCommandSet97_string)]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
[Order(1000)]
internal sealed class RemoveIkvmDependencyCommandHandler : IAsyncCommandGroupHandler
{

    readonly IkvmReferenceWriter _writer;
    readonly Lazy<IkvmDependenciesTreeProvider> _treeProvider;

    [ImportingConstructor]
    public RemoveIkvmDependencyCommandHandler(IkvmReferenceWriter writer, Lazy<IkvmDependenciesTreeProvider> treeProvider)
    {
        _writer = writer;
        _treeProvider = treeProvider;
    }

    public async Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
    {
        if (IsRemove(commandId) == false || nodes.Count == 0 || nodes.All(i => i.Flags.Contains(IkvmDependencyTreeFlags.Reference)) == false)
            return CommandStatusResult.Unhandled;

        var items = GetItems(nodes);
        var removable = items.Count == nodes.Count && await _writer.CanRemoveAsync(items, _treeProvider.Value.GetConfiguredProjects());
        return new CommandStatusResult(true, commandText, removable ? CommandStatus.Enabled | CommandStatus.Supported : CommandStatus.Supported);
    }

    public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
    {
        if (IsRemove(commandId) == false || nodes.All(i => i.Flags.Contains(IkvmDependencyTreeFlags.Reference)) == false)
            return false;

        await _writer.RemoveAsync(GetItems(nodes), _treeProvider.Value.GetConfiguredProjects());
        return true;
    }

    static bool IsRemove(long commandId)
    {
        return commandId == (long)VSConstants.VSStd97CmdID.Remove || commandId == (long)VSConstants.VSStd97CmdID.Delete;
    }

    /// <summary>
    /// Gets the items behind the given nodes, from the contexts of their browse objects.
    /// </summary>
    static List<(string ItemType, string ItemSpec)> GetItems(IImmutableSet<IProjectTree> nodes)
    {
        var result = new List<(string ItemType, string ItemSpec)>();
        foreach (var node in nodes)
            if (node.BrowseObjectProperties?.Context is { ItemType: { Length: > 0 } itemType, ItemName: { Length: > 0 } itemName })
                result.Add((itemType, itemName));

        return result;
    }

}
