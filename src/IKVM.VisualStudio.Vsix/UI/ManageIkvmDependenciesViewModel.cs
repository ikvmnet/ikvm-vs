using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Vsix.ProjectSystem.References;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// State of the Manage IKVM Dependencies dialog: the project's references as they will be after saving.
/// </summary>
sealed class ManageIkvmDependenciesViewModel : ViewModelBase
{

    readonly string _projectDirectory;
    readonly IReadOnlyList<string> _targetFrameworks;
    readonly string? _defaultTargetFramework;
    readonly Func<IReadOnlyCollection<string>, CancellationToken, Task<IReadOnlyDictionary<string, IkvmReferenceDescription>>> _describe;
    IkvmDependencyEntry? _selectedEntry;
    bool _useRelativePaths = true;

    public ManageIkvmDependenciesViewModel(
        string projectDirectory,
        IEnumerable<IkvmReferenceElement> elements,
        IEnumerable<string> targetFrameworks,
        string? defaultTargetFramework,
        Func<IReadOnlyCollection<string>, CancellationToken, Task<IReadOnlyDictionary<string, IkvmReferenceDescription>>> describe)
    {
        _projectDirectory = projectDirectory;
        _describe = describe;

        _targetFrameworks = targetFrameworks.ToList();
        _defaultTargetFramework = defaultTargetFramework;

        foreach (var element in elements)
            Entries.Add(IkvmDependencyEntry.FromElement(element, projectDirectory, _targetFrameworks));

        foreach (var entry in Entries)
            entry.ResolveReferences(name => FindReferenced(entry, name));

        RefreshDependencies();

        foreach (var entry in Entries)
            entry.Changed += (s, e) => Validate();

        Validate();
        SelectedEntry = Entries.FirstOrDefault();
        _ = DescribeAsync(Entries.Where(i => i.IsEditable).ToList());
    }

    public ObservableCollection<IkvmDependencyEntry> Entries { get; } = new ObservableCollection<IkvmDependencyEntry>();

    /// <summary>
    /// Whether the dialog can save: no entry has errors.
    /// </summary>
    public bool CanSave => Entries.All(i => i.HasErrors == false);

    bool _validating;

    /// <summary>
    /// Validates every entry, including the cycles "Depends on" forms across entries.
    /// </summary>
    void Validate()
    {
        if (_validating)
            return;

        _validating = true;
        try
        {
            foreach (var entry in Entries)
                entry.Validate(FindCycles(entry));

            OnPropertyChanged(nameof(CanSave));
        }
        finally
        {
            _validating = false;
        }
    }

    /// <summary>
    /// Finds, for each target framework the entry is used in, a path through "Depends on" that leads back to it.
    /// </summary>
    static List<(string Key, string Path, IkvmDependencyEntry Through)> FindCycles(IkvmDependencyEntry entry)
    {
        var cycles = new List<(string Key, string Path, IkvmDependencyEntry Through)>();
        if (entry.IsRemoved)
            return cycles;

        foreach (var key in entry.UsedKeys)
        {
            var path = new List<IkvmDependencyEntry>() { entry };
            var visited = new HashSet<IkvmDependencyEntry>();
            if (Visit(entry))
                cycles.Add((key, string.Join(" \u2192 ", path.Select(i => i.DisplayName)), path[1]));

            bool Visit(IkvmDependencyEntry from)
            {
                foreach (var next in from.GetReferencedEntries(key))
                {
                    if (next == entry)
                    {
                        path.Add(next);
                        return true;
                    }

                    if (visited.Add(next) == false)
                        continue;

                    path.Add(next);
                    if (Visit(next))
                        return true;

                    path.RemoveAt(path.Count - 1);
                }

                return false;
            }
        }

        return cycles;
    }

    /// <summary>
    /// Whether the project targets more than one framework, so that references can be limited to some.
    /// </summary>
    public bool HasTargetFrameworks => _targetFrameworks.Count > 1;

    public IkvmDependencyEntry? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            Set(ref _selectedEntry, value);
        }
    }

    public bool UseRelativePaths
    {
        get => _useRelativePaths;
        set => Set(ref _useRelativePaths, value);
    }

    /// <summary>
    /// Adds entries for the given paths, skipping ones already listed.
    /// </summary>
    public void AddPaths(IEnumerable<string> paths)
    {
        var added = new List<IkvmDependencyEntry>();

        foreach (var path in paths.Select(Path.GetFullPath))
        {
            if (Entries.Any(i => string.Equals(i.FullPath.TrimEnd('\\'), path.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase)))
                continue;

            // new references apply to the target framework the dialog was opened from, else to all
            var entry = IkvmDependencyEntry.ForNewPath(path, _projectDirectory, _targetFrameworks, _defaultTargetFramework != null ? new[] { _defaultTargetFramework } : Array.Empty<string>());
            entry.Changed += (s, e) => Validate();
            Entries.Add(entry);
            added.Add(entry);
        }

        RefreshDependencies();
        SelectedEntry = added.LastOrDefault() ?? SelectedEntry;
        _ = DescribeAsync(added);
    }

    /// <summary>
    /// Removes a new entry, marks an existing entry for removal, or restores an entry marked for removal.
    /// </summary>
    public void ToggleRemove(IkvmDependencyEntry entry)
    {
        if (entry.IsEditable == false)
            return;

        switch (entry.State)
        {
            case IkvmDependencyState.New:
                Entries.Remove(entry);
                SelectedEntry = Entries.LastOrDefault();
                break;
            case IkvmDependencyState.Existing:
                entry.State = IkvmDependencyState.Removed;
                break;
            case IkvmDependencyState.Removed:
                entry.State = IkvmDependencyState.Existing;
                break;
        }

        RefreshDependencies();
        Validate();
    }

    /// <summary>
    /// Rebuilds each entry's dependency options from the other entries.
    /// </summary>
    void RefreshDependencies()
    {
        foreach (var entry in Entries)
        {
            entry.Dependencies.Clear();
            foreach (var other in Entries.Where(i => i != entry && i.IsRemoved == false))
                entry.Dependencies.Add(new DependencyOption(entry, other));

            entry.Refresh();
        }
    }

    /// <summary>
    /// Finds the entry named in an entry's References metadata.
    /// </summary>
    IkvmDependencyEntry? FindReferenced(IkvmDependencyEntry entry, string name)
    {
        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(Path.Combine(_projectDirectory, name)).TrimEnd('\\');
        }
        catch (ArgumentException)
        {
            fullPath = name;
        }

        return Entries.FirstOrDefault(i => i != entry && (
            string.Equals(i.Original?.Include, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(i.DescribedPath.TrimEnd('\\'), fullPath, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Fills in what the project's IKVM package derives for the given entries.
    /// </summary>
    async Task DescribeAsync(IReadOnlyCollection<IkvmDependencyEntry> entries)
    {
        if (entries.Count == 0)
            return;

        IReadOnlyDictionary<string, IkvmReferenceDescription> descriptions;
        try
        {
            descriptions = await _describe(entries.Select(i => i.DescribedPath).ToList(), CancellationToken.None);
        }
        catch (Exception)
        {
            descriptions = new Dictionary<string, IkvmReferenceDescription>();
        }

        foreach (var entry in entries)
            entry.Description = descriptions.TryGetValue(entry.DescribedPath, out var description) ? description : null;
    }

    /// <summary>
    /// Gets the include an entry is written with.
    /// </summary>
    string GetInclude(IkvmDependencyEntry entry)
    {
        return entry.Original?.Include ?? IkvmDependencyEntry.ToProjectPath(_projectDirectory, entry.DescribedPath, entry.IsDirectory, UseRelativePaths);
    }

    /// <summary>
    /// Gets the changes needed to make the project match the dialog.
    /// </summary>
    public IkvmReferenceChanges GetChanges()
    {
        var removed = new List<IkvmReferenceElement>();
        var updated = new List<IkvmReferenceElementUpdate>();
        var added = new List<IkvmReferenceElement>();

        foreach (var entry in Entries.Where(i => i.IsEditable))
        {
            switch (entry.State)
            {
                case IkvmDependencyState.Removed:
                    removed.Add(entry.Original!);
                    break;
                case IkvmDependencyState.New:
                    added.Add(entry.ToElement(UseRelativePaths, GetInclude));
                    break;
                case IkvmDependencyState.Existing:
                    var element = entry.ToElement(UseRelativePaths, GetInclude);
                    if (IsSame(entry.Original!, element) == false)
                        updated.Add(new IkvmReferenceElementUpdate(entry.Original!, element));
                    break;
            }
        }

        return new IkvmReferenceChanges(removed, updated, added);
    }

    static bool IsSame(IkvmReferenceElement a, IkvmReferenceElement b)
    {
        return TargetFrameworkCondition.AreSame(a.TargetFrameworks, b.TargetFrameworks)
            && a.Metadata.Count == b.Metadata.Count
            && a.Metadata.All(i => b.Metadata.Any(j => IsSame(i, j)));
    }

    static bool IsSame(IkvmReferenceMetadata a, IkvmReferenceMetadata b)
    {
        return string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
            && a.Value == b.Value
            && TargetFrameworkCondition.AreSame(a.TargetFrameworks, b.TargetFrameworks);
    }

}
