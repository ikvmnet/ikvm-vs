using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.ProjectSystem.UI;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// The entry providers that apply to a project, and the items of their types read from it.
/// </summary>
[Export]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
internal sealed class IkvmDependencyService
{

    readonly IProjectThreadingService _threading;
    readonly IkvmReferenceWriter _writer;
    readonly Lazy<IkvmDependenciesTreeProvider> _treeProvider;
    readonly IkvmPackageInstaller _installer;

    /// <summary>
    /// Initializes a new instance, with the entry providers filtered by the capabilities of <paramref name="project"/>.
    /// </summary>
    [ImportingConstructor]
    public IkvmDependencyService(UnconfiguredProject project, IProjectThreadingService threading, IkvmReferenceWriter writer, Lazy<IkvmDependenciesTreeProvider> treeProvider, IkvmPackageInstaller installer)
    {
        _installer = installer;
        Project = project;
        _threading = threading;
        _writer = writer;
        _treeProvider = treeProvider;
        EntryProviders = new OrderPrecedenceImportCollection<IkvmDependencyEntryProvider>(projectCapabilityCheckProvider: project);
    }

    /// <summary>
    /// The project whose dependencies are read and changed.
    /// </summary>
    public UnconfiguredProject Project { get; }

    /// <summary>
    /// Gets the entry providers, filtered by the capabilities of the project.
    /// </summary>
    [ImportMany]
    OrderPrecedenceImportCollection<IkvmDependencyEntryProvider> EntryProviders { get; }

    /// <summary>
    /// Gets the entry providers that apply to the project, one per item type.
    /// </summary>
    public IReadOnlyList<IkvmDependencyEntryProvider> GetEntryProviders()
    {
        return EntryProviders
            .Select(i => i.Value)
            .GroupBy(i => i.ItemType, StringComparer.OrdinalIgnoreCase)
            .Select(i => i.First())
            .ToList();
    }

    /// <summary>
    /// Gets the configured project of each target framework, or a single one under an empty key.
    /// </summary>
    public IReadOnlyDictionary<string, ConfiguredProject> GetConfiguredProjects()
    {
        var projects = _treeProvider.Value.GetConfiguredProjects();
        var result = new Dictionary<string, ConfiguredProject>(StringComparer.OrdinalIgnoreCase);
        foreach (var project in projects)
            result[projects.Length > 1 && project.ProjectConfiguration.Dimensions.TryGetValue("TargetFramework", out var targetFramework) ? targetFramework : ""] = project;

        return result;
    }

    /// <summary>
    /// Gets the target frameworks of the project, if it targets more than one.
    /// </summary>
    public IReadOnlyList<string> GetTargetFrameworks() => _treeProvider.Value.GetTargetFrameworks();

    /// <summary>
    /// Reads the items of the types of the given providers.
    /// </summary>
    public Task<IReadOnlyList<IkvmDependencyElement>> ReadAsync(IReadOnlyList<IkvmDependencyEntryProvider> providers)
    {
        return _writer.ReadAsync(_treeProvider.Value.GetConfiguredProjects(), providers.Select(i => i.ItemType).ToList());
    }

    /// <summary>
    /// Reads the items of the project and starts changing them, with new entries used in the given target framework,
    /// or in all. Returns on the UI thread.
    /// </summary>
    public async Task<IkvmDependencySession> CreateSessionAsync(string? targetFramework)
    {
        var providers = GetEntryProviders();
        var elements = await ReadAsync(providers);
        await _threading.SwitchToUIThread();
        return new IkvmDependencySession(Project, GetConfiguredProjects(), GetTargetFrameworks(), targetFramework, providers, elements, _installer.AddPackageAsync);
    }

    /// <summary>
    /// Gets the descriptions of the add commands of the providers, in order, without reading the project.
    /// </summary>
    public IReadOnlyList<string> GetAddCommandDescriptions()
    {
        var session = new IkvmDependencySession(Project, GetConfiguredProjects(), GetTargetFrameworks(), null, GetEntryProviders(), Array.Empty<IkvmDependencyElement>(), _installer.AddPackageAsync);
        return session.AddCommands.Select(i => i.Description).ToList();
    }

    /// <summary>
    /// Saves the given changes to the project file, telling the user if that fails.
    /// </summary>
    public async Task SaveAsync(IkvmDependencyChanges changes)
    {
        await TaskScheduler.Default;

        try
        {
            await _writer.ApplyAsync(changes);
        }
        catch (Exception e)
        {
            ActivityLog.TryLogError(nameof(IkvmDependencyService), $"Could not save IKVM dependencies: {e}");
            await _threading.SwitchToUIThread();
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, $"Could not save IKVM dependencies: {e.Message}", "IKVM Dependencies", OLEMSGICON.OLEMSGICON_CRITICAL, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }

    /// <summary>
    /// Saves the entries a session added outside the dialog, unless any is not valid, which the user is told.
    /// </summary>
    public async Task SaveAddedAsync(IkvmDependencySession session, IReadOnlyList<IkvmDependencyEntry> added)
    {
        if (added.Count == 0)
            return;

        await _threading.SwitchToUIThread();
        var errors = added.Where(i => i.HasErrors).SelectMany(i => i.Errors.Select(j => $"{i.DisplayName}: {j}")).ToList();
        if (errors.Count > 0)
        {
            var message = "Could not add:\n\n" + string.Join("\n", errors) + "\n\nUse Manage IKVM Dependencies to fix this.";
            VsShellUtilities.ShowMessageBox(ServiceProvider.GlobalProvider, message, "IKVM Dependencies", OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return;
        }

        var changes = session.GetChanges();
        await SaveAsync(new IkvmDependencyChanges(Array.Empty<IkvmDependencyElement>(), Array.Empty<IkvmDependencyElementUpdate>(), changes.Added));
    }

}
