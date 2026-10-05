using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Handles Remove on IKVM references. References defined by MSBuild expressions or wildcards cannot be removed here.
/// </summary>
[ExportCommandGroup(VSConstants.CMDSETID.StandardCommandSet97_string)]
[AppliesTo(IkvmReferenceCapabilities.IkvmReferences)]
[Order(1000)]
internal sealed class RemoveIkvmReferenceCommandHandler : IAsyncCommandGroupHandler
{

    readonly IkvmReferenceWriter _writer;
    readonly Lazy<IkvmDependenciesTreeProvider> _treeProvider;

    [ImportingConstructor]
    public RemoveIkvmReferenceCommandHandler(IkvmReferenceWriter writer, Lazy<IkvmDependenciesTreeProvider> treeProvider)
    {
        _writer = writer;
        _treeProvider = treeProvider;
    }

    public async Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
    {
        if (IsRemove(commandId) == false || nodes.Count == 0 || nodes.All(i => i.Flags.Contains(IkvmDependenciesTreeProvider.ReferenceFlag)) == false)
            return CommandStatusResult.Unhandled;

        var references = GetReferences(nodes);
        var removable = references.Count == nodes.Count && await _writer.CanRemoveAsync(references, _treeProvider.Value.GetConfiguredProjects());
        return new CommandStatusResult(true, commandText, removable ? CommandStatus.Enabled | CommandStatus.Supported : CommandStatus.Supported);
    }

    public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
    {
        if (IsRemove(commandId) == false || nodes.All(i => i.Flags.Contains(IkvmDependenciesTreeProvider.ReferenceFlag)) == false)
            return false;

        await _writer.RemoveAsync(GetReferences(nodes), _treeProvider.Value.GetConfiguredProjects());
        return true;
    }

    static bool IsRemove(long commandId)
    {
        return commandId == (long)VSConstants.VSStd97CmdID.Remove || commandId == (long)VSConstants.VSStd97CmdID.Delete;
    }

    /// <summary>
    /// Gets the references behind the given nodes.
    /// </summary>
    List<IkvmReference> GetReferences(IImmutableSet<IProjectTree> nodes)
    {
        var result = new List<IkvmReference>();
        foreach (var node in nodes)
            if (_treeProvider.Value.TryGetReference(node, out var reference))
                result.Add(reference!);

        return result;
    }

}
