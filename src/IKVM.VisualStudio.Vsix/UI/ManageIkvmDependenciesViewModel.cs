using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.ProjectSystem.UI;
using IKVM.VisualStudio.Vsix.ProjectSystem.References;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// State of the Manage IKVM Dependencies dialog: the project's dependencies as they will be after saving, as entries
/// supplied by the providers of each item type.
/// </summary>
sealed class ManageIkvmDependenciesViewModel : ViewModelBase
{

    readonly IReadOnlyList<IkvmDependencyEntryProvider> _providers;
    readonly ObservableCollection<IkvmDependencyEntry> _entries = new ObservableCollection<IkvmDependencyEntry>();
    IkvmDependencyEntry? _selectedEntry;
    bool _validating;

    public ManageIkvmDependenciesViewModel(
        UnconfiguredProject project,
        IReadOnlyDictionary<string, ConfiguredProject> configuredProjects,
        IReadOnlyList<string> targetFrameworks,
        string? defaultTargetFramework,
        IReadOnlyList<IkvmDependencyEntryProvider> providers,
        IEnumerable<IkvmDependencyElement> elements)
    {
        _providers = providers;

        // new entries apply to the target framework the dialog was opened from, else to all
        Context = new IkvmDependencyEntryContext(project, configuredProjects, targetFrameworks, defaultTargetFramework != null ? new[] { defaultTargetFramework } : Array.Empty<string>(), new ReadOnlyObservableCollection<IkvmDependencyEntry>(_entries));
        AddCommands = providers.SelectMany(i => i.GetAddCommands(Context)).ToList();

        foreach (var element in elements)
            if (GetProvider(element.ItemType) is { } provider)
                _entries.Add(provider.CreateEntry(Context, element));

        foreach (var entry in _entries)
            entry.Changed += OnEntryChanged;

        OnEntriesChanged();
        SelectedEntry = _entries.FirstOrDefault();
        _ = LoadAsync(_entries.ToList());
    }

    /// <summary>
    /// The dialog as the entries see it.
    /// </summary>
    public IkvmDependencyEntryContext Context { get; }

    public ObservableCollection<IkvmDependencyEntry> Entries => _entries;

    /// <summary>
    /// The buttons above the list that add entries.
    /// </summary>
    public IReadOnlyList<IkvmDependencyAddCommand> AddCommands { get; }

    IkvmDependencyEntryProvider? GetProvider(string itemType) => _providers.FirstOrDefault(i => string.Equals(i.ItemType, itemType, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Whether the dialog can save: no entry has errors.
    /// </summary>
    public bool CanSave => _entries.All(i => i.HasErrors == false);

    void OnEntryChanged(object? sender, EventArgs e) => Validate();

    /// <summary>
    /// Validates every entry, as entries can depend on each other.
    /// </summary>
    void Validate()
    {
        if (_validating)
            return;

        _validating = true;
        try
        {
            foreach (var entry in _entries)
                entry.Validate();

            OnPropertyChanged(nameof(CanSave));
        }
        finally
        {
            _validating = false;
        }
    }

    /// <summary>
    /// Tells each entry the entries changed, then validates them.
    /// </summary>
    void OnEntriesChanged()
    {
        _validating = true;
        try
        {
            foreach (var entry in _entries)
                entry.OnEntriesChanged();
        }
        finally
        {
            _validating = false;
        }

        Validate();
    }

    /// <summary>
    /// Whether the project targets more than one framework, so that entries can be limited to some.
    /// </summary>
    public bool HasTargetFrameworks => Context.TargetFrameworks.Count > 1;

    public IkvmDependencyEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => Set(ref _selectedEntry, value);
    }

    public bool UseRelativePaths
    {
        get => Context.UseRelativePaths;
        set
        {
            Context.UseRelativePaths = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Runs an add command, adding the entries it returns.
    /// </summary>
    public async Task AddAsync(IkvmDependencyAddCommand command, Window owner)
    {
        Add(await command.ExecuteAsync(owner));
    }

    /// <summary>
    /// Adds entries for paths dropped on the list, from the providers that take them.
    /// </summary>
    public void AddPaths(IReadOnlyList<string> paths)
    {
        Add(_providers.SelectMany(i => i.CreateEntries(Context, paths)).ToList());
    }

    void Add(IReadOnlyList<IkvmDependencyEntry> added)
    {
        if (added.Count == 0)
            return;

        foreach (var entry in added)
        {
            entry.Changed += OnEntryChanged;
            _entries.Add(entry);
        }

        OnEntriesChanged();
        SelectedEntry = added.Last();
        _ = LoadAsync(added);
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
                entry.Changed -= OnEntryChanged;
                _entries.Remove(entry);
                SelectedEntry = _entries.LastOrDefault();
                break;
            case IkvmDependencyState.Existing:
                entry.State = IkvmDependencyState.Removed;
                break;
            case IkvmDependencyState.Removed:
                entry.State = IkvmDependencyState.Existing;
                break;
        }

        OnEntriesChanged();
    }

    /// <summary>
    /// Lets each provider load what its entries show that takes time to find.
    /// </summary>
    async Task LoadAsync(IReadOnlyList<IkvmDependencyEntry> entries)
    {
        foreach (var provider in _providers)
        {
            var mine = entries.Where(i => string.Equals(i.ItemType, provider.ItemType, StringComparison.OrdinalIgnoreCase)).ToList();
            if (mine.Count == 0)
                continue;

            try
            {
                await provider.LoadAsync(Context, mine, CancellationToken.None);
            }
            catch (Exception e)
            {
                Microsoft.VisualStudio.Shell.ActivityLog.TryLogError(nameof(ManageIkvmDependenciesViewModel), $"{provider.GetType().FullName} could not load entries: {e}");
            }
        }
    }

    /// <summary>
    /// Gets the changes needed to make the project match the dialog.
    /// </summary>
    public IkvmDependencyChanges GetChanges()
    {
        var removed = new List<IkvmDependencyElement>();
        var updated = new List<IkvmDependencyElementUpdate>();
        var added = new List<IkvmDependencyElement>();

        foreach (var entry in _entries.Where(i => i.IsEditable))
        {
            switch (entry.State)
            {
                case IkvmDependencyState.Removed:
                    removed.Add(entry.Original!);
                    break;
                case IkvmDependencyState.New:
                    added.Add(entry.ToElement());
                    break;
                case IkvmDependencyState.Existing:
                    var element = entry.ToElement();
                    if (IsSame(entry.Original!, element) == false)
                        updated.Add(new IkvmDependencyElementUpdate(entry.Original!, element));
                    break;
            }
        }

        return new IkvmDependencyChanges(removed, updated, added);
    }

    static bool IsSame(IkvmDependencyElement a, IkvmDependencyElement b)
    {
        return string.Equals(a.Include, b.Include, StringComparison.Ordinal)
            && TargetFrameworkCondition.AreSame(a.TargetFrameworks, b.TargetFrameworks)
            && a.Metadata.Count == b.Metadata.Count
            && a.Metadata.All(i => b.Metadata.Any(j => IsSame(i, j)));
    }

    static bool IsSame(IkvmDependencyMetadata a, IkvmDependencyMetadata b)
    {
        return string.Equals(a.Name, b.Name, StringComparison.OrdinalIgnoreCase)
            && a.Value == b.Value
            && TargetFrameworkCondition.AreSame(a.TargetFrameworks, b.TargetFrameworks);
    }

}
