using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IKVM.VisualStudio.ProjectSystem;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Handles Open Containing Folder on a JAR file or class directory: opens Explorer with it selected.
/// </summary>
[ExportCommandGroup(IkvmDependencyCommandIds.CommandSetString)]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
internal sealed class OpenContainingFolderCommandHandler : IAsyncCommandGroupHandler
{

    public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
    {
        if (commandId != IkvmDependencyCommandIds.OpenContainingFolder || nodes.Count != 1 || nodes.First().Flags.Contains(IkvmDependencyTreeFlags.JarFile) == false)
            return Task.FromResult(CommandStatusResult.Unhandled);

        var enabled = GetPath(nodes.First()) is { } path && (File.Exists(path) || Directory.Exists(path));
        return Task.FromResult(new CommandStatusResult(true, commandText, enabled ? CommandStatus.Enabled | CommandStatus.Supported : CommandStatus.Supported));
    }

    public Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
    {
        if (commandId != IkvmDependencyCommandIds.OpenContainingFolder || nodes.Count != 1 || GetPath(nodes.First()) is not { } path)
            return Task.FromResult(false);

        Process.Start("explorer.exe", $"/select,\"{path.TrimEnd('\\', '/')}\"");
        return Task.FromResult(true);
    }

    /// <summary>
    /// Gets the path of a JAR file node, named by its browse object.
    /// </summary>
    static string? GetPath(IProjectTree node)
    {
        return node.BrowseObjectProperties?.Context.ItemName is { Length: > 0 } path ? path : null;
    }

}
