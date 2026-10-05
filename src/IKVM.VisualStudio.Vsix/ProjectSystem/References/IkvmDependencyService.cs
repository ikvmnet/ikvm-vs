using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.ProjectSystem.UI;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// The entry providers that apply to a project, and the items of their types read from it.
/// </summary>
[Export]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
internal sealed class IkvmDependencyService
{

    readonly IkvmReferenceWriter _writer;
    readonly Lazy<IkvmDependenciesTreeProvider> _treeProvider;

    [ImportingConstructor]
    public IkvmDependencyService(UnconfiguredProject project, IkvmReferenceWriter writer, Lazy<IkvmDependenciesTreeProvider> treeProvider)
    {
        Project = project;
        _writer = writer;
        _treeProvider = treeProvider;
        EntryProviders = new OrderPrecedenceImportCollection<IkvmDependencyEntryProvider>(projectCapabilityCheckProvider: project);
    }

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
    /// Saves the given changes to the project file.
    /// </summary>
    public Task ApplyAsync(IkvmDependencyChanges changes) => _writer.ApplyAsync(changes);

}
