using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.ProjectSystem.UI;
using IKVM.VisualStudio.Vsix.Imaging;
using IKVM.VisualStudio.Vsix.UI;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.Win32;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Supplies the <c>IkvmReference</c> items of a project to the Manage IKVM Dependencies dialog: JARs and class
/// directories.
/// </summary>
[Export(typeof(IkvmDependencyEntryProvider))]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
[Order(1000)]
internal sealed class IkvmReferenceEntryProvider : IkvmDependencyEntryProvider
{

    readonly IkvmReferenceDescriber _describer;

    [ImportingConstructor]
    public IkvmReferenceEntryProvider(IkvmReferenceDescriber describer)
    {
        _describer = describer;
    }

    public override string ItemType => IkvmReferenceRules.ItemType;

    public override IkvmDependencyEntry CreateEntry(IkvmDependencyEntryContext context, IkvmDependencyElement element)
    {
        return JarDependencyEntry.FromElement(context, element);
    }

    public override IReadOnlyList<IkvmDependencyAddCommand> GetAddCommands(IkvmDependencyEntryContext context)
    {
        return new[]
        {
            new IkvmDependencyAddCommand("JARs...", IkvmMonikers.JarFile, "Add JAR files", owner => Task.FromResult(AddJars(context, owner))),
            new IkvmDependencyAddCommand("Folder...", IkvmMonikers.ClassFolder, "Add class folder", owner => Task.FromResult(AddFolder(context))),
        };
    }

    IReadOnlyList<IkvmDependencyEntry> AddJars(IkvmDependencyEntryContext context, Window owner)
    {
        var dialog = new OpenFileDialog() { Title = "Add JAR Files", Filter = "Java archives (*.jar)|*.jar|All files (*.*)|*.*", Multiselect = true, InitialDirectory = context.ProjectDirectory };
        return dialog.ShowDialog(owner) == true ? CreateEntriesCore(context, dialog.FileNames) : Array.Empty<IkvmDependencyEntry>();
    }

    IReadOnlyList<IkvmDependencyEntry> AddFolder(IkvmDependencyEntryContext context)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog() { Description = "Select a folder of .class files", SelectedPath = context.ProjectDirectory, ShowNewFolderButton = false };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? CreateEntriesCore(context, new[] { dialog.SelectedPath }) : Array.Empty<IkvmDependencyEntry>();
    }

    public override IReadOnlyList<IkvmDependencyEntry> CreateEntries(IkvmDependencyEntryContext context, IReadOnlyList<string> paths)
    {
        return CreateEntriesCore(context, paths.Where(i => Directory.Exists(i) || string.Equals(Path.GetExtension(i), ".jar", StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Creates entries for the given paths, skipping ones already listed.
    /// </summary>
    static IReadOnlyList<IkvmDependencyEntry> CreateEntriesCore(IkvmDependencyEntryContext context, IEnumerable<string> paths)
    {
        var listed = context.Entries.OfType<JarDependencyEntry>().Select(i => i.FullPath.TrimEnd('\\')).ToList();
        var result = new List<IkvmDependencyEntry>();

        foreach (var path in paths.Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase))
            if (listed.Contains(path.TrimEnd('\\'), StringComparer.OrdinalIgnoreCase) == false)
                result.Add(JarDependencyEntry.ForNewPath(context, path));

        return result;
    }

    /// <summary>
    /// Fills in what the IKVM package of the project derives for the given entries.
    /// </summary>
    public override async Task LoadAsync(IkvmDependencyEntryContext context, IReadOnlyList<IkvmDependencyEntry> entries, CancellationToken cancellationToken)
    {
        var jars = entries.OfType<JarDependencyEntry>().Where(i => i.IsEditable).ToList();
        if (jars.Count == 0)
            return;

        IReadOnlyDictionary<string, IkvmReferenceDescription> descriptions;
        try
        {
            descriptions = await _describer.DescribeAsync(jars.Select(i => i.DescribedPath).ToList(), cancellationToken);
        }
        catch (Exception e) when (e is not OperationCanceledException)
        {
            descriptions = new Dictionary<string, IkvmReferenceDescription>();
        }

        foreach (var jar in jars)
            jar.Description = descriptions.TryGetValue(jar.DescribedPath, out var description) ? description : null;
    }

}
