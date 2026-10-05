using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

using IKVM.VisualStudio.ProjectSystem;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Handles "Manage IKVM Dependencies..." in .NET projects that could use IKVM but do not yet: offers to add the IKVM
/// package, which brings the IkvmReferences capability, and with it the IKVM Dependencies node and dialog.
/// </summary>
[ExportCommandGroup(IkvmDependencyCommandIds.CommandSetString)]
[AppliesTo(Capability)]
internal sealed class AddIkvmPackageCommandHandler : IAsyncCommandGroupHandler
{

    /// <summary>
    /// SDK-style C#, Visual Basic and F# projects that take NuGet packages, without IKVM.
    /// </summary>
    const string Capability = "(CSharp | VB | FSharp) & PackageReferences & !" + IkvmDependencyCapabilities.IkvmReferences;

    const string PackageId = "IKVM";

    readonly IkvmPackageInstaller _installer;

    /// <summary>
    /// Initializes a new instance with the installer that offers to add the IKVM package.
    /// </summary>
    [ImportingConstructor]
    public AddIkvmPackageCommandHandler(IkvmPackageInstaller installer)
    {
        _installer = installer;
    }

    /// <summary>
    /// Enables the command wherever it is offered.
    /// </summary>
    public Task<CommandStatusResult> GetCommandStatusAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, string? commandText, CommandStatus progressiveStatus)
    {
        if (commandId != IkvmDependencyCommandIds.ManageIkvmDependencies)
            return Task.FromResult(CommandStatusResult.Unhandled);

        return Task.FromResult(new CommandStatusResult(true, commandText, CommandStatus.Enabled | CommandStatus.Supported));
    }

    /// <summary>
    /// Offers to add the IKVM package and, if it was added, tells the user the dependencies become available once it
    /// is restored.
    /// </summary>
    public async Task<bool> TryHandleCommandAsync(IImmutableSet<IProjectTree> nodes, long commandId, bool focused, long commandExecuteOptions, IntPtr variantArgIn, IntPtr variantArgOut)
    {
        if (commandId != IkvmDependencyCommandIds.ManageIkvmDependencies)
            return false;

        if (await _installer.AddPackageAsync(PackageId, "This project does not use IKVM yet. IKVM dependencies, such as JARs and Maven references, need the IKVM package."))
        {
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, "IKVM was added. Once the package is restored, the project shows IKVM Dependencies, and Manage IKVM Dependencies edits them.", "IKVM Dependencies", OLEMSGICON.OLEMSGICON_INFO, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }

        return true;
    }

}
