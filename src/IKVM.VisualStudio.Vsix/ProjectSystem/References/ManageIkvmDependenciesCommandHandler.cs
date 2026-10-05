using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using IKVM.VisualStudio.Vsix.Commands;
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
[ExportCommandGroup(JavaReferenceCommandIds.CommandSetString)]
[AppliesTo(IkvmReferenceCapabilities.IkvmReferences)]
internal sealed class ManageIkvmDependenciesCommandHandler : IAsyncCommandGroupHandler
{

    readonly UnconfiguredProject _project;
    readonly IProjectThreadingService _threading;
    readonly JavaReferenceWriter _writer;
    readonly JavaReferenceDescriber _describer;
    readonly Lazy<JavaReferencesTreeProvider> _treeProvider;

    [ImportingConstructor]
    public ManageIkvmDependenciesCommandHandler(UnconfiguredProject project, IProjectThreadingService threading, JavaReferenceWriter writer, JavaReferenceDescriber describer, Lazy<JavaReferencesTreeProvider> treeProvider)
    {
        _project = project;
        _threading = threading;
        _writer = writer;
        _describer = describer;
        _treeProvider = treeProvider;
    }

    public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
    {
        if (commandId != JavaReferenceCommandIds.AddJavaReference)
            return Task.FromResult(CommandStatusResult.Unhandled);

        return Task.FromResult(new CommandStatusResult(true, commandText, CommandStatus.Enabled | CommandStatus.Supported));
    }

    public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
    {
        if (commandId != JavaReferenceCommandIds.AddJavaReference)
            return false;

        // invoked from a target framework folder: new references default to that framework
        var targetFramework = nodes.FirstOrDefault(i => i.Flags.Contains(JavaReferencesTreeProvider.TargetFrameworkFlag))?.Caption;
        var projectDirectory = Path.GetDirectoryName(_project.FullPath)!;
        IReadOnlyList<IkvmReferenceElement> elements;
        try
        {
            elements = await _writer.ReadAsync(_treeProvider.Value.GetConfiguredProjects());
        }
        catch (Exception e)
        {
            ActivityLog.TryLogError(nameof(ManageIkvmDependenciesCommandHandler), $"Could not read IKVM dependencies: {e}");
            throw;
        }
        var targetFrameworks = _treeProvider.Value.GetTargetFrameworks();

        await _threading.SwitchToUIThread();

        var dialog = new ManageIkvmDependenciesDialog(projectDirectory, elements, targetFrameworks, targetFramework, _describer.DescribeAsync);
        if (dialog.ShowModal() != true)
            return true;

        var changes = dialog.GetChanges();
        await TaskScheduler.Default;

        try
        {
            await _writer.ApplyAsync(changes);
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
