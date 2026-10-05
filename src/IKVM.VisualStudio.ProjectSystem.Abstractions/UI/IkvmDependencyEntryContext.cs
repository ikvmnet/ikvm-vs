using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// The Manage IKVM Dependencies dialog, as the entries in it see it.
/// </summary>
public sealed class IkvmDependencyEntryContext
{

    internal IkvmDependencyEntryContext(
        UnconfiguredProject project,
        IReadOnlyDictionary<string, ConfiguredProject> configuredProjects,
        IReadOnlyList<string> targetFrameworks,
        IReadOnlyList<string> defaultTargetFrameworks,
        ReadOnlyObservableCollection<IkvmDependencyEntry> entries)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        ConfiguredProjects = configuredProjects ?? throw new ArgumentNullException(nameof(configuredProjects));
        TargetFrameworks = targetFrameworks ?? throw new ArgumentNullException(nameof(targetFrameworks));
        DefaultTargetFrameworks = defaultTargetFrameworks ?? throw new ArgumentNullException(nameof(defaultTargetFrameworks));
        Entries = entries ?? throw new ArgumentNullException(nameof(entries));
    }

    /// <summary>
    /// Gets the project.
    /// </summary>
    public UnconfiguredProject Project { get; }

    /// <summary>
    /// Gets the directory of the project.
    /// </summary>
    public string ProjectDirectory => Path.GetDirectoryName(Project.FullPath)!;

    /// <summary>
    /// Gets the configured project of each target framework, or a single one under an empty key.
    /// </summary>
    public IReadOnlyDictionary<string, ConfiguredProject> ConfiguredProjects { get; }

    /// <summary>
    /// Gets the target frameworks of the project, if it targets more than one.
    /// </summary>
    public IReadOnlyList<string> TargetFrameworks { get; }

    /// <summary>
    /// Gets the target frameworks new entries are used in, or empty for all: the one the dialog was opened from.
    /// </summary>
    public IReadOnlyList<string> DefaultTargetFrameworks { get; }

    /// <summary>
    /// Gets every entry in the dialog.
    /// </summary>
    public ReadOnlyObservableCollection<IkvmDependencyEntry> Entries { get; }

    /// <summary>
    /// Gets whether paths of new entries are written relative to the project, as the user chose.
    /// </summary>
    public bool UseRelativePaths { get; internal set; } = true;

}
