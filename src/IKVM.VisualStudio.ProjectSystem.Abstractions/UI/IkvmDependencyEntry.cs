using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// An item in the Manage IKVM Dependencies dialog: one already in the project, or one being added. The dialog lists
/// the entry, lets the user choose the target frameworks it is used in and those whose settings are viewed and edited,
/// and shows the entry's own view for those settings.
/// </summary>
/// <remarks>
/// Derive from <see cref="IkvmDependencyEntry{TValues}"/>, which keeps the entry's values by target framework.
/// </remarks>
public abstract class IkvmDependencyEntry : ViewModelBase
{

    const string VariesPlaceholderText = "Different values: a value typed here replaces them";

    readonly IReadOnlyList<string> _keys;
    readonly HashSet<string> _editing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    readonly List<TargetFrameworkOption> _options = new List<TargetFrameworkOption>();
    IkvmDependencyState _state;
    bool _allTargetFrameworks;
    IReadOnlyList<string> _errors = Array.Empty<string>();
    FrameworkElement? _view;

    /// <summary>
    /// Initializes an entry for an item already in the project, or for a new one when <paramref name="original"/> is
    /// <c>null</c>. A new entry is used in the target frameworks the dialog adds to.
    /// </summary>
    protected IkvmDependencyEntry(IkvmDependencyEntryContext context, string itemType, IkvmDependencyElement? original)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        ItemType = itemType ?? throw new ArgumentNullException(nameof(itemType));
        Original = original;
        _state = original == null ? IkvmDependencyState.New : IkvmDependencyState.Existing;

        var targetFrameworks = context.TargetFrameworks;
        var selected = original?.TargetFrameworks ?? context.DefaultTargetFrameworks;

        // values are kept by target framework, or under an empty key when the project has a single one
        _keys = targetFrameworks.Count > 1 ? targetFrameworks : new[] { "" };

        // all target frameworks unless limited to some, which shows each of them on; list any the element names that
        // the project does not
        _allTargetFrameworks = selected.Count == 0;
        foreach (var name in targetFrameworks.Concat(selected).Distinct(StringComparer.OrdinalIgnoreCase))
            _options.Add(new TargetFrameworkOption(name, _allTargetFrameworks || selected.Contains(name, StringComparer.OrdinalIgnoreCase)));

        // edit every target framework the entry is used in
        foreach (var key in _keys.Where(AppliesTo))
            _editing.Add(key);

        if (targetFrameworks.Count > 1)
        {
            Chips.Add(new TargetFrameworkChip("All", true, OnToggleUsed, OnToggleEditing));
            foreach (var option in _options)
                Chips.Add(new TargetFrameworkChip(option.Name, false, OnToggleUsed, OnToggleEditing));
        }
    }

    /// <summary>
    /// Gets the dialog the entry is in.
    /// </summary>
    public IkvmDependencyEntryContext Context { get; }

    /// <summary>
    /// Gets the item type of the entry.
    /// </summary>
    public string ItemType { get; }

    /// <summary>
    /// Gets the element as read from the project, for existing entries.
    /// </summary>
    public IkvmDependencyElement? Original { get; }

    // shown in the list and above the details

    /// <summary>
    /// Gets the name of the entry, in the list and above its details.
    /// </summary>
    public abstract string DisplayName { get; }

    /// <summary>
    /// Gets the line shown under the name in the list, if any.
    /// </summary>
    public virtual string? Subtitle => null;

    /// <summary>
    /// Gets the line shown under the name above the details, if any.
    /// </summary>
    public virtual string? Location => null;

    /// <summary>
    /// Gets the icon of the entry.
    /// </summary>
    public abstract ImageMoniker Icon { get; }

    /// <summary>
    /// Gets the view of the entry's settings, shown under its target frameworks with the entry as its data context.
    /// </summary>
    public FrameworkElement View => _view ??= CreateView();

    /// <summary>
    /// Creates the view of the entry's settings, once. It merges <see cref="IkvmDependencyStyles.Uri"/> into its
    /// resources to look like the rest of the dialog.
    /// </summary>
    protected abstract FrameworkElement CreateView();

    // state

    public IkvmDependencyState State
    {
        get => _state;
        internal set
        {
            if (Set(ref _state, value))
                Refresh();
        }
    }

    public bool IsNew => _state == IkvmDependencyState.New;

    public bool IsRemoved => _state == IkvmDependencyState.Removed;

    /// <summary>
    /// Gets whether the entry's element can be edited: it is new, or written in the project file without MSBuild
    /// expressions.
    /// </summary>
    public bool IsEditable => Original == null || Original.IsEditable;

    /// <summary>
    /// Gets whether the entry's settings can be edited: it can be, and a target framework's settings are being edited.
    /// </summary>
    public bool CanEdit => IsEditable && IsRemoved == false && HasEditing;

    public string GroupName => IsNew ? "New" : "Referenced";

    /// <summary>
    /// Sorts referenced entries before new ones.
    /// </summary>
    public int GroupOrder => IsNew ? 1 : 0;

    /// <summary>
    /// Gets whether the entry is imported from another file, rather than written in the project file.
    /// </summary>
    public bool IsImported => Original?.DefinedIn != null;

    /// <summary>
    /// Gets where an imported entry comes from, shown when hovering it in the list.
    /// </summary>
    public string? ImportedToolTip => Original?.DefinedIn is { } path ? $"Imported from {path}. Edit that file to change it." : null;

    /// <summary>
    /// Gets a note about the entry shown above its details, if any. Overrides add their own after the base's.
    /// </summary>
    public virtual string? Status
    {
        get
        {
            if (IsRemoved)
                return "Will be removed from the project when you save.";
            if (Original?.DefinedIn != null)
                return $"Imported from {Original.DefinedIn}. Edit that file to change it.";
            if (IsEditable == false)
                return "Defined with MSBuild expressions or conditions. Edit the project file to change it.";

            return null;
        }
    }

    // target frameworks

    /// <summary>
    /// Gets "All", then each target framework: whether the entry is used in it, and whether it is being edited.
    /// </summary>
    public ObservableCollection<TargetFrameworkChip> Chips { get; } = new ObservableCollection<TargetFrameworkChip>();

    /// <summary>
    /// Adds the entry to or takes it out of a chip's target framework. "All" applies it to every target framework,
    /// including ones added later; clearing it keeps them, as a list.
    /// </summary>
    void OnToggleUsed(TargetFrameworkChip chip)
    {
        if (IsEditable == false || IsRemoved)
            return;

        if (chip.IsAll)
        {
            _allTargetFrameworks = _allTargetFrameworks == false;
            if (_allTargetFrameworks)
                foreach (var option in _options)
                    option.IsChecked = true;
        }
        else
        {
            var option = _options.First(i => i.Name == chip.Name);
            option.IsChecked = option.IsChecked == false;

            if (option.IsChecked == false)
                _allTargetFrameworks = false;
            else if (_options.All(i => i.IsChecked) && (Original == null || Original.TargetFrameworks.Count == 0))
                _allTargetFrameworks = true;
        }

        Refresh();
    }

    /// <summary>
    /// Starts or stops viewing and editing a chip's target framework, or all of them.
    /// </summary>
    void OnToggleEditing(TargetFrameworkChip chip)
    {
        if (chip.IsAll)
        {
            var all = _keys.All(_editing.Contains);
            foreach (var key in _keys)
                if (all)
                    _editing.Remove(key);
                else
                    _editing.Add(key);
        }
        else if (_editing.Remove(chip.Name) == false)
        {
            _editing.Add(chip.Name);
        }

        Refresh();
    }

    /// <summary>
    /// Gets the target frameworks the entry is limited to, or empty for all.
    /// </summary>
    public IReadOnlyList<string> TargetFrameworks => _allTargetFrameworks ? Array.Empty<string>() : _options.Where(i => i.IsChecked).Select(i => i.Name).ToList();

    /// <summary>
    /// Gets the keys values are kept under: each target framework, or an empty one for a project with a single one.
    /// </summary>
    protected IReadOnlyList<string> Keys => _keys;

    /// <summary>
    /// Gets whether the entry applies to the target framework with the given key.
    /// </summary>
    protected bool AppliesTo(string key)
    {
        if (key.Length == 0)
            return true;
        if (IsEditable == false && Original != null && Original.Evaluations.Count > 0)
            return Original.Evaluations.ContainsKey(key);

        return _allTargetFrameworks || _options.Any(i => i.IsChecked && string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the keys of the target frameworks whose settings are shown and edited: those being edited, or the only
    /// one. Values set for a target framework the entry is not used in are kept, for when it is.
    /// </summary>
    protected IReadOnlyList<string> ShownKeys => _keys.Count == 1 ? _keys : _keys.Where(_editing.Contains).ToList();

    /// <summary>
    /// Gets the keys of the target frameworks the entry is used in.
    /// </summary>
    public IReadOnlyList<string> UsedKeys => _keys.Where(AppliesTo).ToList();

    /// <summary>
    /// Gets whether any target framework's settings are being edited.
    /// </summary>
    public bool HasEditing => ShownKeys.Count > 0;

    /// <summary>
    /// Gets what to do when no target framework's settings are being edited.
    /// </summary>
    public string NothingEditingText => "Click a target framework's name above to view and edit its settings.";

    /// <summary>
    /// Gets the badges shown at the bottom right of the entry in the list, when it is used in only some target
    /// frameworks: each of them, or how many past two.
    /// </summary>
    public IReadOnlyList<string> Badges
    {
        get
        {
            if (_keys.Count == 1)
                return Array.Empty<string>();

            var used = UsedKeys;
            if (used.Count == _keys.Count)
                return Array.Empty<string>();

            return used.Count < 3 ? used : new[] { $"{used.Count} frameworks" };
        }
    }

    /// <summary>
    /// Gets the placeholder of a text field whose value differs between the target frameworks being edited.
    /// </summary>
    protected static string VariesPlaceholder => VariesPlaceholderText;

    // the original element

    /// <summary>
    /// Gets the include of the original element for one target framework: as written, or as evaluated for entries
    /// the dialog cannot edit.
    /// </summary>
    protected string GetOriginalInclude(string key)
    {
        if (Original == null)
            return "";

        if (Original.IsEditable == false && Original.Evaluations.TryGetValue(key, out var evaluation))
            return evaluation.Include;

        return Original.Include;
    }

    /// <summary>
    /// Gets a metadata value of the original element for one target framework: as written, or as evaluated for
    /// entries the dialog cannot edit.
    /// </summary>
    protected string GetOriginalMetadata(string name, string key)
    {
        if (Original == null)
            return "";

        if (Original.IsEditable == false && Original.Evaluations.Count > 0)
            return Original.Evaluations.TryGetValue(key, out var evaluation) && evaluation.Metadata.TryGetValue(name, out var value) ? value : "";

        return Original.GetMetadata(name, key);
    }

    // changes

    /// <summary>
    /// Raises change notifications for everything shown, after a change.
    /// </summary>
    public void Refresh()
    {
        foreach (var chip in Chips)
        {
            var keys = chip.IsAll ? _keys : new[] { chip.Name };
            chip.IsUsed = chip.IsAll ? _allTargetFrameworks || (IsEditable == false && keys.All(AppliesTo)) : AppliesTo(chip.Name);
            chip.IsEditing = keys.All(_editing.Contains);
            chip.CanChangeUsed = IsEditable && IsRemoved == false;
        }

        OnRefresh();
        OnPropertyChanged(string.Empty);
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called on every change, before change notifications are raised, to update state derived from the values.
    /// </summary>
    protected virtual void OnRefresh()
    {

    }

    /// <summary>
    /// Raised when anything about the entry changes, so that the dialog can validate again.
    /// </summary>
    public event EventHandler? Changed;

    /// <summary>
    /// Called when entries are added to or removed from the dialog, or marked for removal, for entries that refer to
    /// others. <see cref="IkvmDependencyEntryContext.Entries"/> holds them all.
    /// </summary>
    protected internal virtual void OnEntriesChanged()
    {

    }

    /// <summary>
    /// Gets the items the context menu of the entry in the list shows, before the items of the dialog, such as Remove.
    /// </summary>
    public virtual IReadOnlyList<IkvmDependencyMenuItem> GetMenuItems() => Array.Empty<IkvmDependencyMenuItem>();

    // validation

    /// <summary>
    /// Gets what is wrong with the entry; the dialog cannot save while any entry has errors.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors;

    public bool HasErrors => _errors.Count > 0;

    public string? ErrorToolTip => HasErrors ? string.Join("\n", _errors) : null;

    /// <summary>
    /// Validates the entry.
    /// </summary>
    internal void Validate()
    {
        var validation = new IkvmDependencyValidation(_keys.Count > 1);

        if (IsEditable && IsRemoved == false)
        {
            if (UsedKeys.Count == 0)
                validation.AddError("Not used in any target framework.");

            Validate(validation);
        }

        _errors = validation.Errors;
        OnPropertyChanged(string.Empty);
    }

    /// <summary>
    /// Checks the settings of an entry that can be edited and is not being removed, for the target frameworks it is
    /// used in. Called after every change to any entry.
    /// </summary>
    protected virtual void Validate(IkvmDependencyValidation validation)
    {

    }

    // saving

    /// <summary>
    /// Gets the element for the entry's current state.
    /// </summary>
    internal IkvmDependencyElement ToElement()
    {
        if (Original != null && Original.IsEditable == false)
            return Original;

        var builder = new IkvmDependencyElementBuilder(Original, UsedKeys);
        Save(builder);
        return builder.Build(ItemType, TargetFrameworks);
    }

    /// <summary>
    /// Writes the entry's include and metadata. Metadata of the original element not written is kept.
    /// </summary>
    protected abstract void Save(IkvmDependencyElementBuilder builder);

}
