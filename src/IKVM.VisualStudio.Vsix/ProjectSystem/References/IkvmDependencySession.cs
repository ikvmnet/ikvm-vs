using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.ProjectSystem.UI;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// The dependencies of a project as entries from the providers of each item type, being changed: by the Manage IKVM
/// Dependencies dialog, or by an add command or a drop in Solution Explorer.
/// </summary>
sealed class IkvmDependencySession
{

    readonly ObservableCollection<IkvmDependencyEntry> _entries = new ObservableCollection<IkvmDependencyEntry>();
    bool _validating;

    public IkvmDependencySession(
        UnconfiguredProject project,
        IReadOnlyDictionary<string, ConfiguredProject> configuredProjects,
        IReadOnlyList<string> targetFrameworks,
        string? defaultTargetFramework,
        IReadOnlyList<IkvmDependencyEntryProvider> providers,
        IEnumerable<IkvmDependencyElement> elements,
        Func<string, string, Task<bool>> addPackage)
    {
        Providers = providers;

        // new entries apply to the target framework the session was started from, else to all
        Context = new IkvmDependencyEntryContext(project, configuredProjects, targetFrameworks, defaultTargetFramework != null ? new[] { defaultTargetFramework } : Array.Empty<string>(), new ReadOnlyObservableCollection<IkvmDependencyEntry>(_entries), addPackage);
        AddCommands = providers.SelectMany(i => i.GetAddCommands(Context)).ToList();

        foreach (var element in elements)
            if (GetProvider(element.ItemType) is { } provider)
                _entries.Add(provider.CreateEntry(Context, element));

        foreach (var entry in _entries)
            entry.Changed += OnEntryChanged;

        NotifyEntriesChanged();
    }

    /// <summary>
    /// The project as the entries see it.
    /// </summary>
    public IkvmDependencyEntryContext Context { get; }

    public ObservableCollection<IkvmDependencyEntry> Entries => _entries;

    public IReadOnlyList<IkvmDependencyEntryProvider> Providers { get; }

    /// <summary>
    /// The add commands of the providers, in order.
    /// </summary>
    public IReadOnlyList<IkvmDependencyAddCommand> AddCommands { get; }

    IkvmDependencyEntryProvider? GetProvider(string itemType) => Providers.FirstOrDefault(i => string.Equals(i.ItemType, itemType, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Raised after the entries are validated, which they are after every change.
    /// </summary>
    public event EventHandler? Validated;

    void OnEntryChanged(object? sender, EventArgs e) => Validate();

    /// <summary>
    /// Validates every entry, as entries can depend on each other.
    /// </summary>
    public void Validate()
    {
        if (_validating)
            return;

        _validating = true;
        try
        {
            foreach (var entry in _entries)
                entry.Validate();
        }
        finally
        {
            _validating = false;
        }

        Validated?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Tells each entry the entries changed, then validates them.
    /// </summary>
    public void NotifyEntriesChanged()
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
    /// Runs an add command, adding the entries it returns.
    /// </summary>
    public async Task<IReadOnlyList<IkvmDependencyEntry>> AddAsync(IkvmDependencyAddCommand command, Window owner)
    {
        var added = await command.ExecuteAsync(owner);
        Add(added);
        return added;
    }

    /// <summary>
    /// Adds entries for dropped paths, from the providers that take them.
    /// </summary>
    public IReadOnlyList<IkvmDependencyEntry> AddPaths(IReadOnlyList<string> paths)
    {
        var added = Providers.SelectMany(i => i.CreateEntries(Context, paths)).ToList();
        Add(added);
        return added;
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

        NotifyEntriesChanged();
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
                break;
            case IkvmDependencyState.Existing:
                entry.State = IkvmDependencyState.Removed;
                break;
            case IkvmDependencyState.Removed:
                entry.State = IkvmDependencyState.Existing;
                break;
        }

        NotifyEntriesChanged();
    }

    /// <summary>
    /// Lets each provider load what its entries show that takes time to find.
    /// </summary>
    public async Task LoadAsync(IReadOnlyList<IkvmDependencyEntry> entries)
    {
        foreach (var provider in Providers)
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
                ActivityLog.TryLogError(nameof(IkvmDependencySession), $"{provider.GetType().FullName} could not load entries: {e}");
            }
        }
    }

    /// <summary>
    /// Gets the changes needed to make the project match the entries.
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
