using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

using IKVM.VisualStudio.Vsix.ProjectSystem.References;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// A JAR or class directory in the Manage IKVM Dependencies dialog: one already in the project, or one being added.
/// Its values are kept by target framework; the dialog shows and edits them for the target frameworks in view.
/// </summary>
sealed class IkvmDependencyEntry : ViewModelBase
{

    const string AssemblyNameKey = "AssemblyName";
    const string AssemblyVersionKey = "AssemblyVersion";
    const string CompileKey = "Compile";
    const string SourcesKey = "Sources";
    const string ReferencesKey = "References";

    static readonly string[] EditedKeys = { AssemblyNameKey, AssemblyVersionKey, CompileKey, SourcesKey, ReferencesKey };

    /// <summary>
    /// Creates an entry for an item already in the project.
    /// </summary>
    public static IkvmDependencyEntry FromElement(IkvmReferenceElement element, string projectDirectory, IReadOnlyList<string> targetFrameworks)
    {
        var fullPath = JavaReferenceWriter.IsLiteral(element.Include) ? Resolve(projectDirectory, element.Include) : element.Include;
        var entry = new IkvmDependencyEntry(fullPath, element, IkvmDependencyState.Existing, projectDirectory, targetFrameworks, element.TargetFrameworks);

        foreach (var key in entry._keys)
        {
            var values = entry._values[key];
            values.AssemblyName = entry.GetOriginalMetadata(AssemblyNameKey, key);
            values.AssemblyVersion = entry.GetOriginalMetadata(AssemblyVersionKey, key);

            foreach (var path in Split(entry.GetOriginalMetadata(CompileKey, key)).Select(i => Resolve(projectDirectory, i)))
                if (values.Classes.Contains(path, StringComparer.OrdinalIgnoreCase) == false)
                    values.Classes.Add(path);

            // Compile not set compiles the entry itself
            if (values.Classes.Count == 0)
                values.Classes.Add(entry.GetFullPath(key));

            foreach (var path in Split(entry.GetOriginalMetadata(SourcesKey, key)).Select(i => Resolve(projectDirectory, i)))
                if (values.Sources.Contains(path, StringComparer.OrdinalIgnoreCase) == false)
                    values.Sources.Add(path);
        }

        entry.Refresh();
        return entry;
    }

    /// <summary>
    /// Creates an entry for a JAR or directory being added.
    /// </summary>
    public static IkvmDependencyEntry ForNewPath(string fullPath, string projectDirectory, IReadOnlyList<string> targetFrameworks, IReadOnlyList<string> selected)
    {
        var entry = new IkvmDependencyEntry(fullPath, null, IkvmDependencyState.New, projectDirectory, targetFrameworks, selected);

        foreach (var values in entry._values.Values)
            values.Classes.Add(fullPath);

        if (entry.Info.SourcesPath != null)
            foreach (var values in entry._values.Values)
                values.Sources.Add(entry.Info.SourcesPath);

        entry.Refresh();
        return entry;
    }

    static IEnumerable<string> Split(string value) => value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).Where(i => i.Length > 0);

    static string Resolve(string projectDirectory, string path)
    {
        try
        {
            return Path.GetFullPath(Path.Combine(projectDirectory, path));
        }
        catch (ArgumentException)
        {
            return path;
        }
    }

    readonly string _projectDirectory;
    readonly string _fullPath;
    readonly IReadOnlyList<string> _keys;
    readonly Dictionary<string, IkvmDependencyValues> _values = new Dictionary<string, IkvmDependencyValues>(StringComparer.OrdinalIgnoreCase);
    readonly HashSet<string> _editing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    IkvmDependencyState _state;
    bool _allTargetFrameworks;
    JavaReferenceDescription? _description;
    bool _isDescribed;
    JarInfo _info;

    IkvmDependencyEntry(string fullPath, IkvmReferenceElement? original, IkvmDependencyState state, string projectDirectory, IReadOnlyList<string> targetFrameworks, IReadOnlyList<string> selected)
    {
        _projectDirectory = projectDirectory;
        _fullPath = fullPath;
        Original = original;
        _state = state;

        // values are kept by target framework, or under an empty key when the project has a single one
        _keys = targetFrameworks.Count > 1 ? targetFrameworks : new[] { "" };
        foreach (var key in _keys)
            _values[key] = new IkvmDependencyValues();

        // all target frameworks unless limited to some, which shows each of them on; list any the element names that
        // the project does not
        _allTargetFrameworks = selected.Count == 0;
        foreach (var name in targetFrameworks.Concat(selected).Distinct(StringComparer.OrdinalIgnoreCase))
            TargetFrameworkOptions.Add(new TargetFrameworkOption(name, _allTargetFrameworks || selected.Contains(name, StringComparer.OrdinalIgnoreCase)));

        // edit every target framework the entry is used in
        foreach (var key in _keys.Where(AppliesTo))
            _editing.Add(key);

        if (targetFrameworks.Count > 1)
        {
            Chips.Add(new TargetFrameworkChip("All", true, OnToggleUsed, OnToggleEditing));
            foreach (var option in TargetFrameworkOptions)
                Chips.Add(new TargetFrameworkChip(option.Name, false, OnToggleUsed, OnToggleEditing));
        }

        _info = JarInfo.Read(FullPath);
    }

    /// <summary>
    /// The element as read from the project, for existing entries.
    /// </summary>
    public IkvmReferenceElement? Original { get; }

    /// <summary>
    /// The entry's path for the target frameworks in view: as evaluated for items MSBuild evaluates differently by
    /// target framework, which shows the unevaluated include when those differ.
    /// </summary>
    public string FullPath
    {
        get
        {
            if (IsEditable || Original == null || Original.Evaluations.Count == 0)
                return _fullPath;

            var paths = ShownKeys.Select(GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return paths.Count == 1 ? paths[0] : Original.Include;
        }
    }

    /// <summary>
    /// The path the entry has for one target framework.
    /// </summary>
    string GetFullPath(string key)
    {
        if (IsEditable == false && Original != null && Original.Evaluations.TryGetValue(key, out var evaluation))
            return Resolve(_projectDirectory, evaluation.Include);

        return _fullPath;
    }

    /// <summary>
    /// The path the dialog asks the project's IKVM package to describe.
    /// </summary>
    public string DescribedPath => _fullPath;

    public JarInfo Info => _info;

    public bool IsDirectory => _info.IsDirectory;

    public string DisplayName => Path.GetFileName(FullPath.TrimEnd('\\', '/'));

    public string DirectoryName => Path.GetDirectoryName(FullPath.TrimEnd('\\', '/')) ?? "";

    public IkvmDependencyState State
    {
        get => _state;
        set
        {
            if (Set(ref _state, value))
            {
                OnPropertyChanged(nameof(IsRemoved));
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(GroupName));
                OnPropertyChanged(nameof(Status));
            }
        }
    }

    public bool IsNew => _state == IkvmDependencyState.New;

    public bool IsRemoved => _state == IkvmDependencyState.Removed;

    /// <summary>
    /// Whether the entry's element can be edited: it is new, or written in the project file without MSBuild
    /// expressions.
    /// </summary>
    public bool IsEditable => Original == null || Original.IsEditable;

    /// <summary>
    /// Whether the entry's settings can be edited: it can be, and a target framework's settings are being edited.
    /// </summary>
    public bool CanEdit => IsEditable && IsRemoved == false && HasEditing;

    public string GroupName => IsNew ? "New" : "Referenced";

    /// <summary>
    /// Sorts referenced entries before new ones.
    /// </summary>
    public int GroupOrder => IsNew ? 1 : 0;

    /// <summary>
    /// Whether the entry applies to all target frameworks, including ones added to the project later, rather than to
    /// a list of them.
    /// </summary>
    public bool AllTargetFrameworks => _allTargetFrameworks;

    public ObservableCollection<TargetFrameworkOption> TargetFrameworkOptions { get; } = new ObservableCollection<TargetFrameworkOption>();

    /// <summary>
    /// "All", then each target framework: whether the entry is used in it, and whether it is being edited.
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
                foreach (var option in TargetFrameworkOptions)
                    option.IsChecked = true;
        }
        else
        {
            var option = TargetFrameworkOptions.First(i => i.Name == chip.Name);
            option.IsChecked = option.IsChecked == false;

            if (option.IsChecked == false)
                _allTargetFrameworks = false;
            else if (TargetFrameworkOptions.All(i => i.IsChecked) && (Original == null || Original.TargetFrameworks.Count == 0))
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
    /// Target frameworks the entry is limited to, or empty for all.
    /// </summary>
    public IReadOnlyList<string> TargetFrameworks => _allTargetFrameworks ? Array.Empty<string>() : TargetFrameworkOptions.Where(i => i.IsChecked).Select(i => i.Name).ToList();

    /// <summary>
    /// Gets whether the entry applies to the target framework with the given key.
    /// </summary>
    bool AppliesTo(string key)
    {
        if (key.Length == 0)
            return true;
        if (IsEditable == false && Original != null && Original.Evaluations.Count > 0)
            return Original.Evaluations.ContainsKey(key);

        return _allTargetFrameworks || TargetFrameworkOptions.Any(i => i.IsChecked && string.Equals(i.Name, key, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Target frameworks whose settings are shown and edited: those being edited, or the only one. Values set for a
    /// target framework the entry is not used in are kept, for when it is.
    /// </summary>
    IReadOnlyList<string> ShownKeys => _keys.Count == 1 ? _keys : _keys.Where(_editing.Contains).ToList();

    /// <summary>
    /// Whether any target framework's settings are being edited.
    /// </summary>
    public bool HasEditing => ShownKeys.Count > 0;

    /// <summary>
    /// What to do when no target framework's settings are being edited.
    /// </summary>
    public string NothingEditingText => "Click a target framework's name above to view and edit its settings.";

    /// <summary>
    /// Whether the entry is imported from another file, rather than written in the project file.
    /// </summary>
    public bool IsImported => Original?.DefinedIn != null;

    /// <summary>
    /// Where an imported entry comes from, shown when hovering it in the list.
    /// </summary>
    public string? ImportedToolTip => Original?.DefinedIn is { } path ? $"Imported from {path}. Edit that file to change it." : null;

    /// <summary>
    /// Shown at the bottom right of the entry in the list, when it is used in only some target frameworks: each of
    /// them, or how many past two.
    /// </summary>
    public IReadOnlyList<string> Badges
    {
        get
        {
            if (_keys.Count == 1)
                return Array.Empty<string>();

            var used = _keys.Where(AppliesTo).ToList();
            if (used.Count == _keys.Count)
                return Array.Empty<string>();

            return used.Count < 3 ? used : new[] { $"{used.Count} frameworks" };
        }
    }

    // assembly name and version

    public string AssemblyName
    {
        get => Common(i => i.AssemblyName);
        set => Apply(i => i.AssemblyName = value);
    }

    public bool AssemblyNameVaries => Varies(i => i.AssemblyName);

    public string? AssemblyNameVariations => Variations(i => i.AssemblyName);

    public string AssemblyNamePlaceholder => AssemblyNameVaries ? VariesPlaceholder : Placeholder(_description?.AssemblyName);

    public string AssemblyVersion
    {
        get => Common(i => i.AssemblyVersion);
        set => Apply(i => i.AssemblyVersion = value);
    }

    public bool AssemblyVersionVaries => Varies(i => i.AssemblyVersion);

    public string? AssemblyVersionVariations => Variations(i => i.AssemblyVersion);

    public string AssemblyVersionPlaceholder => AssemblyVersionVaries ? VariesPlaceholder : Placeholder(_description?.AssemblyVersion);

    const string VariesPlaceholder = "Different values: a value typed here replaces them";

    string Placeholder(string? detected) => IsEditable == false ? "Not set" : _isDescribed == false ? "Detecting..." : detected != null ? $"{detected} (detected)" : "Not detected: set a value";

    // classes and sources

    /// <summary>
    /// The classes compiled into the assembly for the target frameworks being edited, in order.
    /// </summary>
    public IReadOnlyList<PathItem> ClassItems => GetPathItems(i => i.Classes);

    public bool ClassesVary => Varies(i => Join(i.Classes));

    public string? ClassesVariations => Variations(i => Join(i.Classes.Select(Path.GetFileName)));

    /// <summary>
    /// The source archives for the target frameworks being edited, in order.
    /// </summary>
    public IReadOnlyList<PathItem> SourceItems => GetPathItems(i => i.Sources);

    /// <summary>
    /// Gets the paths of a list for the target frameworks being edited: the first one's order, followed by any the
    /// others add.
    /// </summary>
    List<string> GetPathOrder(Func<IkvmDependencyValues, List<string>> list)
    {
        return ShownKeys.SelectMany(i => list(_values[i])).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    List<PathItem> GetPathItems(Func<IkvmDependencyValues, List<string>> list)
    {
        return GetPathOrder(list).Select(path =>
        {
            var keys = ShownKeys.Where(i => list(_values[i]).Contains(path, StringComparer.OrdinalIgnoreCase)).ToList();
            return new PathItem(path, keys.Count < ShownKeys.Count ? "Only for " + string.Join(", ", keys) : null);
        }).ToList();
    }

    /// <summary>
    /// Moves paths of a list to a position, for the target frameworks being edited, adding those not yet listed.
    /// </summary>
    void MovePaths(Func<IkvmDependencyValues, List<string>> list, IReadOnlyList<string> paths, int index)
    {
        var order = GetPathOrder(list);
        order.RemoveAll(i => paths.Contains(i, StringComparer.OrdinalIgnoreCase));
        order.InsertRange(Math.Max(0, Math.Min(index, order.Count)), paths);

        Apply(i =>
        {
            var values = list(i);
            foreach (var path in paths)
                if (values.Contains(path, StringComparer.OrdinalIgnoreCase) == false)
                    values.Add(path);

            // the order shown, then any paths not shown
            var sorted = values.OrderBy(j => order.FindIndex(k => string.Equals(k, j, StringComparison.OrdinalIgnoreCase)) is var k && k >= 0 ? k : int.MaxValue).ToList();
            values.Clear();
            values.AddRange(sorted);
        });
    }

    void RemovePath(Func<IkvmDependencyValues, List<string>> list, string path)
    {
        Apply(i => list(i).RemoveAll(j => string.Equals(j, path, StringComparison.OrdinalIgnoreCase)));
    }

    public void MoveClass(string path, int index) => MovePaths(i => i.Classes, new[] { path }, index);

    public void RemoveClass(string path) => RemovePath(i => i.Classes, path);

    public void MoveSource(string path, int index) => MovePaths(i => i.Sources, new[] { path }, index);

    public void RemoveSource(string path) => RemovePath(i => i.Sources, path);

    public bool SourcesVary => Varies(i => Join(i.Sources));

    public string? SourcesVariations => Variations(i => Join(i.Sources.Select(Path.GetFileName)));

    static string Join(IEnumerable<string> values) => string.Join(", ", values);

    /// <summary>
    /// Adds classes at a position for the target frameworks being edited.
    /// </summary>
    public void AddClasses(IEnumerable<string> paths, int index = int.MaxValue)
    {
        MovePaths(i => i.Classes, paths.ToList(), index);
    }

    /// <summary>
    /// Adds sources at a position for the target frameworks being edited.
    /// </summary>
    public void AddSources(IEnumerable<string> paths, int index = int.MaxValue)
    {
        MovePaths(i => i.Sources, paths.ToList(), index);
    }

    // references

    public ObservableCollection<DependencyOption> Dependencies { get; } = new ObservableCollection<DependencyOption>();

    /// <summary>
    /// The entries this entry references for any target framework being edited.
    /// </summary>
    public IReadOnlyList<DependencyOption> ReferencedDependencies => GetReferenceOrder()
        .Select(i => Dependencies.FirstOrDefault(j => j.Target == i))
        .Where(i => i != null)
        .ToList()!;

    /// <summary>
    /// Gets the entries referenced for the target frameworks being edited, in order: the first one's order, followed
    /// by any the others add.
    /// </summary>
    List<IkvmDependencyEntry> GetReferenceOrder()
    {
        var order = new List<IkvmDependencyEntry>();
        foreach (var key in ShownKeys)
            foreach (var target in _values[key].References)
                if (target.IsRemoved == false && order.Contains(target) == false)
                    order.Add(target);

        return order;
    }

    /// <summary>
    /// Moves a referenced entry to the given position, for the target frameworks being edited; one not yet referenced
    /// is added there.
    /// </summary>
    public void MoveReference(IkvmDependencyEntry target, int index)
    {
        var order = GetReferenceOrder();
        order.Remove(target);
        order.Insert(Math.Max(0, Math.Min(index, order.Count)), target);

        Apply(i =>
        {
            if (i.References.Contains(target) == false)
                i.References.Add(target);

            // the order shown, then any references removed from the dialog
            var sorted = i.References.OrderBy(j => order.IndexOf(j) is var k && k >= 0 ? k : int.MaxValue).ToList();
            i.References.Clear();
            i.References.AddRange(sorted);
        });
    }

    /// <summary>
    /// The entries that can be added: those not referenced for every target framework being edited.
    /// </summary>
    public IReadOnlyList<DependencyOption> AddableDependencies => Dependencies.Where(i => i.IsChecked != true).ToList();

    /// <summary>
    /// Whether some referenced entry is referenced for only some of the target frameworks being edited.
    /// </summary>
    public bool DependenciesVary => Dependencies.Any(i => i.IsChecked == null);

    /// <summary>
    /// Lists the target frameworks being edited in which the entry references the given entry, when that is only some
    /// of them.
    /// </summary>
    public string? GetReferencesNote(IkvmDependencyEntry target)
    {
        return GetReferences(target) == null ? "Only for " + string.Join(", ", ShownKeys.Where(i => _values[i].References.Contains(target))) : null;
    }

    /// <summary>
    /// Resolves the entries named by the entry's <c>References</c> metadata.
    /// </summary>
    public void ResolveReferences(Func<string, IkvmDependencyEntry?> resolve)
    {
        if (Original == null)
            return;

        foreach (var key in _keys)
            foreach (var name in Split(GetOriginalMetadata(ReferencesKey, key)))
                if (resolve(name) is { } target && target != this && _values[key].References.Contains(target) == false)
                    _values[key].References.Add(target);
    }

    /// <summary>
    /// Gets whether the entry references the given entry: for all target frameworks in view, none, or some (<c>null</c>).
    /// </summary>
    public bool? GetReferences(IkvmDependencyEntry target)
    {
        var count = ShownKeys.Count(i => _values[i].References.Contains(target));
        return count == 0 ? false : count == ShownKeys.Count ? true : null;
    }

    /// <summary>
    /// Sets whether the entry references the given entry, for the target frameworks in view.
    /// </summary>
    public void SetReferences(IkvmDependencyEntry target, bool value)
    {
        Apply(i =>
        {
            if (value == false)
                i.References.Remove(target);
            else if (i.References.Contains(target) == false)
                i.References.Add(target);
        });
    }

    // values by target framework

    /// <summary>
    /// Gets a value of the original element for one target framework: as written, or as evaluated for entries the
    /// dialog cannot edit.
    /// </summary>
    string GetOriginalMetadata(string name, string key)
    {
        if (Original == null)
            return "";

        if (Original.IsEditable == false && Original.Evaluations.Count > 0)
            return Original.Evaluations.TryGetValue(key, out var evaluation) && evaluation.Metadata.TryGetValue(name, out var value) ? value : "";

        return Original.GetMetadata(name, key);
    }

    string Common(Func<IkvmDependencyValues, string> get)
    {
        var values = ShownKeys.Select(i => get(_values[i])).Distinct().ToList();
        return values.Count == 1 ? values[0] : "";
    }

    bool Varies(Func<IkvmDependencyValues, string> get)
    {
        return ShownKeys.Select(i => get(_values[i])).Distinct().Count() > 1;
    }

    /// <summary>
    /// Lists a value that differs by target framework, one line each, such as <c>net472: a</c>; <c>null</c> when it
    /// does not differ.
    /// </summary>
    string? Variations(Func<IkvmDependencyValues, string> get)
    {
        if (Varies(get) == false)
            return null;

        return string.Join("\n", ShownKeys.Select(i => (Key: i, Value: get(_values[i]))).Select(i => $"{i.Key}: {(i.Value.Length > 0 ? i.Value : "not set")}"));
    }

    /// <summary>
    /// Changes the values of every target framework being edited.
    /// </summary>
    void Apply(Action<IkvmDependencyValues> change)
    {
        foreach (var key in ShownKeys)
            change(_values[key]);

        Refresh();
    }

    /// <summary>
    /// Raises change notifications for everything shown for the target frameworks in view.
    /// </summary>
    public void Refresh()
    {
        foreach (var option in Dependencies)
            option.Refresh();

        foreach (var chip in Chips)
        {
            var keys = chip.IsAll ? _keys : new[] { chip.Name };
            chip.IsUsed = chip.IsAll ? _allTargetFrameworks || (IsEditable == false && keys.All(AppliesTo)) : AppliesTo(chip.Name);
            chip.IsEditing = keys.All(_editing.Contains);
            chip.CanChangeUsed = IsEditable && IsRemoved == false;
        }

        _info = JarInfo.Read(FullPath);

        foreach (var name in new[] {
            nameof(FullPath), nameof(DisplayName), nameof(DirectoryName), nameof(IsDirectory), nameof(Info), nameof(HasEditing), nameof(NothingEditingText), nameof(CanEdit), nameof(Status), nameof(Badges), nameof(TargetFrameworks),
            nameof(AssemblyName), nameof(AssemblyNameVaries), nameof(AssemblyNameVariations), nameof(AssemblyNamePlaceholder),
            nameof(AssemblyVersion), nameof(AssemblyVersionVaries), nameof(AssemblyVersionVariations), nameof(AssemblyVersionPlaceholder),
            nameof(ClassItems), nameof(ClassesVary), nameof(ClassesVariations), nameof(SourceItems), nameof(SourcesVary), nameof(SourcesVariations), nameof(ReferencedDependencies), nameof(AddableDependencies), nameof(DependenciesVary) }.Concat(FrameNames))
            OnPropertyChanged(name);

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raised when anything about the entry changes, so that the dialog can validate again.
    /// </summary>
    public event EventHandler? Changed;

    // validation

    IReadOnlyList<string> _errors = Array.Empty<string>();
    bool _dependenciesCycle;
    HashSet<IkvmDependencyEntry> _cycleTargets = new HashSet<IkvmDependencyEntry>();

    /// <summary>
    /// What is wrong with the entry; the dialog cannot save while any entry has errors.
    /// </summary>
    public IReadOnlyList<string> Errors => _errors;

    public bool HasErrors => _errors.Count > 0;

    public string? ErrorToolTip => HasErrors ? string.Join("\n", _errors) : null;

    /// <summary>
    /// Whether Compile is empty for a target framework being edited.
    /// </summary>
    public bool CompileInvalid => HasErrors && ShownKeys.Any(i => AppliesTo(i) && _values[i].Classes.Count == 0);

    public bool AssemblyNameInvalid => HasErrors && ShownKeys.Any(i => AppliesTo(i) && IsAssemblyNameValid(_values[i].AssemblyName) == false);

    public bool AssemblyVersionInvalid => HasErrors && ShownKeys.Any(i => AppliesTo(i) && IsAssemblyVersionValid(_values[i].AssemblyVersion) == false);

    /// <summary>
    /// Whether "Depends on" leads back to the entry.
    /// </summary>
    public bool DependenciesInvalid => _dependenciesCycle;

    // the state each field shows: not valid, varying between the target frameworks being edited, or neither

    static FieldState FrameState(bool invalid, bool varies) => invalid ? FieldState.Error : varies ? FieldState.Varies : FieldState.Normal;

    public FieldState CompileFrame => FrameState(CompileInvalid, ClassesVary);

    public FieldState SourcesFrame => FrameState(false, SourcesVary);

    public FieldState AssemblyNameFrame => FrameState(AssemblyNameInvalid, AssemblyNameVaries);

    public FieldState AssemblyVersionFrame => FrameState(AssemblyVersionInvalid, AssemblyVersionVaries);

    public FieldState DependenciesFrame => FrameState(DependenciesInvalid, DependenciesVary);

    static readonly string[] FrameNames = { nameof(CompileFrame), nameof(SourcesFrame), nameof(AssemblyNameFrame), nameof(AssemblyVersionFrame), nameof(DependenciesFrame) };

    static bool IsAssemblyNameValid(string value) => value.Trim().IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    static bool IsAssemblyVersionValid(string value) => string.IsNullOrWhiteSpace(value) || Version.TryParse(value.Trim(), out _);

    /// <summary>
    /// Whether depending on the given entry leads back to this one.
    /// </summary>
    public bool IsCycleThrough(IkvmDependencyEntry target) => _cycleTargets.Contains(target);

    /// <summary>
    /// The target frameworks the entry is used in, for validating across entries.
    /// </summary>
    internal IEnumerable<string> UsedKeys => _keys.Where(AppliesTo);

    /// <summary>
    /// The entries this one references for a target framework, for validating across entries.
    /// </summary>
    internal IEnumerable<IkvmDependencyEntry> GetReferencedEntries(string key) => _values[key].References.Where(i => i.IsRemoved == false);

    /// <summary>
    /// Checks the entry's own settings and takes the cycles found through "Depends on" across entries.
    /// </summary>
    internal void Validate(IReadOnlyList<(string Key, string Path, IkvmDependencyEntry Through)> cycles)
    {
        var errors = new List<string>();

        if (IsEditable && IsRemoved == false)
        {
            var used = UsedKeys.ToList();
            if (used.Count == 0)
                errors.Add("Not used in any target framework.");

            AddError(errors, used.Where(i => _values[i].Classes.Count == 0), "Compile is empty: add a JAR or class folder to compile.");
            AddError(errors, used.Where(i => IsAssemblyNameValid(_values[i].AssemblyName) == false), "The assembly name contains characters a file name cannot.");
            AddError(errors, used.Where(i => IsAssemblyVersionValid(_values[i].AssemblyVersion) == false), "The assembly version is not a version, such as 1.2.3.4.");

            foreach (var cycle in cycles.GroupBy(i => i.Path))
                AddError(errors, cycle.Select(i => i.Key), $"Depends on itself: {cycle.Key}.");
        }

        _errors = errors;
        _dependenciesCycle = IsEditable && IsRemoved == false && cycles.Count > 0;
        _cycleTargets = _dependenciesCycle ? new HashSet<IkvmDependencyEntry>(cycles.Select(i => i.Through)) : new HashSet<IkvmDependencyEntry>();

        foreach (var option in Dependencies)
            option.Refresh();

        foreach (var name in new[] { nameof(Errors), nameof(HasErrors), nameof(ErrorToolTip), nameof(CompileInvalid), nameof(AssemblyNameInvalid), nameof(AssemblyVersionInvalid), nameof(DependenciesInvalid) }.Concat(FrameNames))
            OnPropertyChanged(name);
    }

    /// <summary>
    /// Adds an error for the given target frameworks, naming them when the project has several.
    /// </summary>
    void AddError(List<string> errors, IEnumerable<string> keys, string message)
    {
        var list = keys.Distinct().ToList();
        if (list.Count == 0)
            return;

        errors.Add(_keys.Count > 1 ? $"{message} ({string.Join(", ", list)})" : message);
    }

    // description and status

    /// <summary>
    /// What the project's IKVM package derives for this entry, once known.
    /// </summary>
    public JavaReferenceDescription? Description
    {
        get => _description;
        set
        {
            _description = value;
            _isDescribed = true;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AssemblyNamePlaceholder));
            OnPropertyChanged(nameof(AssemblyVersionPlaceholder));
            OnPropertyChanged(nameof(Status));
        }
    }

    /// <summary>
    /// A note about the entry shown above its details, if any.
    /// </summary>
    public string? Status
    {
        get
        {
            if (IsRemoved)
                return "Will be removed from the project when you save.";
            if (Original?.DefinedIn != null)
                return $"Imported from {Original.DefinedIn}. Edit that file to change it.";
            if (IsEditable == false)
                return "Defined with MSBuild expressions or conditions. Edit the project file to change it.";
            if (_description?.IsResolved == false)
                return _description.Diagnostic;

            return null;
        }
    }

    // saving

    /// <summary>
    /// Gets the element for the entry's current state. Values the same for every target framework the entry applies
    /// to are written for all of them; values that differ are written for each.
    /// </summary>
    public IkvmReferenceElement ToElement(bool useRelativePaths, Func<IkvmDependencyEntry, string> getInclude)
    {
        if (Original != null && Original.IsEditable == false)
            return Original;

        // keep metadata the dialog does not edit
        var metadata = new List<IkvmReferenceMetadata>();
        if (Original != null)
            metadata.AddRange(Original.Metadata.Where(i => EditedKeys.Contains(i.Name, StringComparer.OrdinalIgnoreCase) == false));

        var keys = _keys.Where(AppliesTo).ToList();
        AddMetadata(metadata, keys, AssemblyNameKey, i => i.AssemblyName.Trim());
        AddMetadata(metadata, keys, AssemblyVersionKey, i => i.AssemblyVersion.Trim());
        // Compile is left unset when it would list only the entry itself, its default
        AddMetadata(metadata, keys, CompileKey, i => i.Classes.Count == 0 || (i.Classes.Count == 1 && string.Equals(i.Classes[0], _fullPath, StringComparison.OrdinalIgnoreCase)) ? "" : string.Join(";", i.Classes.Select(j => ToProjectPath(_projectDirectory, j, Directory.Exists(j), useRelativePaths))));
        AddMetadata(metadata, keys, SourcesKey, i => string.Join(";", i.Sources.Select(j => ToProjectPath(_projectDirectory, j, false, useRelativePaths))));
        AddMetadata(metadata, keys, ReferencesKey, i => string.Join(";", i.References.Where(j => j.IsRemoved == false).Select(getInclude)));

        var include = Original?.Include ?? ToProjectPath(_projectDirectory, _fullPath, IsDirectory, useRelativePaths);
        return new IkvmReferenceElement(include, TargetFrameworks, metadata, true);
    }

    void AddMetadata(List<IkvmReferenceMetadata> metadata, IReadOnlyList<string> keys, string name, Func<IkvmDependencyValues, string> get)
    {
        var values = keys.Select(i => (Key: i, Value: get(_values[i]))).ToList();

        // the same everywhere: one value for all target frameworks
        if (values.Select(i => i.Value).Distinct().Count() <= 1)
        {
            var value = values.Select(i => i.Value).FirstOrDefault() ?? "";
            if (value.Length > 0)
                metadata.Add(new IkvmReferenceMetadata(name, value, Array.Empty<string>()));

            return;
        }

        foreach (var group in values.Where(i => i.Value.Length > 0).GroupBy(i => i.Value))
            metadata.Add(new IkvmReferenceMetadata(name, group.Key, group.Select(i => i.Key).ToList()));
    }

    /// <summary>
    /// Gets a path as written into the project file.
    /// </summary>
    public static string ToProjectPath(string projectDirectory, string fullPath, bool isDirectory, bool useRelativePaths)
    {
        var path = useRelativePaths ? MakeRelative(projectDirectory, fullPath) : fullPath;
        if (isDirectory && path.EndsWith("\\", StringComparison.Ordinal) == false)
            path += "\\";

        return path;
    }

    static string MakeRelative(string baseDirectory, string path)
    {
        if (string.Equals(Path.GetPathRoot(baseDirectory), Path.GetPathRoot(path), StringComparison.OrdinalIgnoreCase) == false)
            return path;

        var baseUri = new Uri(baseDirectory.TrimEnd('\\') + "\\");
        var pathUri = new Uri(path);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(pathUri).ToString()).Replace('/', '\\');
    }

}
