using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

using IKVM.VisualStudio.ProjectSystem.UI;
using IKVM.VisualStudio.Vsix.ProjectSystem.References;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// State of the Manage IKVM Dependencies dialog: the project's dependencies as they will be after saving, and the one
/// selected.
/// </summary>
sealed class ManageIkvmDependenciesViewModel : ViewModelBase
{

    readonly IkvmDependencySession _session;
    IkvmDependencyEntry? _selectedEntry;

    /// <summary>
    /// Creates the state for a session, selecting the first entry and starting to load what the entries show that
    /// takes time to find.
    /// </summary>
    public ManageIkvmDependenciesViewModel(IkvmDependencySession session)
    {
        _session = session;
        _session.Validated += (s, e) => OnPropertyChanged(nameof(CanSave));

        SelectedEntry = Entries.FirstOrDefault();
        _ = _session.LoadAsync(Entries.ToList());
    }

    /// <summary>
    /// The entries of the dialog: those already in the project and those being added.
    /// </summary>
    public ObservableCollection<IkvmDependencyEntry> Entries => _session.Entries;

    /// <summary>
    /// The buttons above the list that add entries.
    /// </summary>
    public IReadOnlyList<IkvmDependencyAddCommand> AddCommands => _session.AddCommands;

    /// <summary>
    /// Whether the dialog can save: no entry has errors.
    /// </summary>
    public bool CanSave => Entries.All(i => i.HasErrors == false);

    /// <summary>
    /// Whether the project targets more than one framework, so that entries can be limited to some.
    /// </summary>
    public bool HasTargetFrameworks => _session.Context.TargetFrameworks.Count > 1;

    /// <summary>
    /// The entry whose details are shown.
    /// </summary>
    public IkvmDependencyEntry? SelectedEntry
    {
        get => _selectedEntry;
        set => Set(ref _selectedEntry, value);
    }

    /// <summary>
    /// Whether paths of new entries are written relative to the project, as the user chose.
    /// </summary>
    public bool UseRelativePaths
    {
        get => _session.Context.UseRelativePaths;
        set
        {
            _session.Context.UseRelativePaths = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Runs an add command, selecting the last entry it adds.
    /// </summary>
    public async Task AddAsync(IkvmDependencyAddCommand command, Window owner)
    {
        Select(await _session.AddAsync(command, owner));
    }

    /// <summary>
    /// Adds entries for paths dropped on the list.
    /// </summary>
    public void AddPaths(IReadOnlyList<string> paths)
    {
        Select(_session.AddPaths(paths));
    }

    /// <summary>
    /// Selects the last of the entries just added and starts loading what they show that takes time to find.
    /// </summary>
    void Select(IReadOnlyList<IkvmDependencyEntry> added)
    {
        if (added.Count == 0)
            return;

        SelectedEntry = added.Last();
        _ = _session.LoadAsync(added);
    }

    /// <summary>
    /// Removes a new entry, marks an existing entry for removal, or restores an entry marked for removal.
    /// </summary>
    public void ToggleRemove(IkvmDependencyEntry entry)
    {
        _session.ToggleRemove(entry);
        if (Entries.Contains(entry) == false)
            SelectedEntry = Entries.LastOrDefault();
    }

    /// <summary>
    /// Gets the changes needed to make the project match the dialog.
    /// </summary>
    public IkvmDependencyChanges GetChanges() => _session.GetChanges();

}
