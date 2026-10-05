using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;

using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.ProjectSystem.UI;
using IKVM.VisualStudio.Vsix.Imaging;
using IKVM.VisualStudio.Vsix.ProjectSystem.References;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// A JAR or class directory in the Manage IKVM Dependencies dialog: an <c>IkvmReference</c> item already in the
/// project, or one being added.
/// </summary>
sealed class JarDependencyEntry : IkvmDependencyEntry<JarDependencyValues>
{

    const string AssemblyNameKey = "AssemblyName";
    const string AssemblyVersionKey = "AssemblyVersion";
    const string CompileKey = "Compile";
    const string SourcesKey = "Sources";
    const string ReferencesKey = "References";

    /// <summary>
    /// Creates an entry for an item already in the project.
    /// </summary>
    public static JarDependencyEntry FromElement(IkvmDependencyEntryContext context, IkvmDependencyElement element)
    {
        var fullPath = IkvmReferenceWriter.IsLiteral(element.Include) ? Resolve(context.ProjectDirectory, element.Include) : element.Include;
        var entry = new JarDependencyEntry(context, fullPath, element);

        foreach (var key in entry.Keys)
        {
            var values = entry.GetValues(key);
            values.AssemblyName = entry.GetOriginalMetadata(AssemblyNameKey, key);
            values.AssemblyVersion = entry.GetOriginalMetadata(AssemblyVersionKey, key);

            foreach (var path in Split(entry.GetOriginalMetadata(CompileKey, key)).Select(i => Resolve(context.ProjectDirectory, i)))
                if (values.Classes.Contains(path, StringComparer.OrdinalIgnoreCase) == false)
                    values.Classes.Add(path);

            // Compile not set compiles the entry itself
            if (values.Classes.Count == 0)
                values.Classes.Add(entry.GetFullPath(key));

            foreach (var path in Split(entry.GetOriginalMetadata(SourcesKey, key)).Select(i => Resolve(context.ProjectDirectory, i)))
                if (values.Sources.Contains(path, StringComparer.OrdinalIgnoreCase) == false)
                    values.Sources.Add(path);
        }

        entry.Refresh();
        return entry;
    }

    /// <summary>
    /// Creates an entry for a JAR or directory being added.
    /// </summary>
    public static JarDependencyEntry ForNewPath(IkvmDependencyEntryContext context, string fullPath)
    {
        var entry = new JarDependencyEntry(context, fullPath, null);

        foreach (var key in entry.Keys)
        {
            entry.GetValues(key).Classes.Add(fullPath);
            if (entry.Info.SourcesPath != null)
                entry.GetValues(key).Sources.Add(entry.Info.SourcesPath);
        }

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

    readonly string _fullPath;
    IkvmReferenceDescription? _description;
    bool _isDescribed;
    bool _referencesResolved;
    JarInfo _info;

    JarDependencyEntry(IkvmDependencyEntryContext context, string fullPath, IkvmDependencyElement? original) :
        base(context, IkvmReferenceRules.ItemType, original)
    {
        _fullPath = fullPath;
        _info = JarInfo.Read(FullPath);
    }

    protected override FrameworkElement CreateView() => new JarDependencyView() { DataContext = this };

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
        if (IsEditable == false && Original != null && Original.Evaluations.ContainsKey(key))
            return Resolve(Context.ProjectDirectory, GetOriginalInclude(key));

        return _fullPath;
    }

    /// <summary>
    /// The path the dialog asks the project's IKVM package to describe.
    /// </summary>
    public string DescribedPath => _fullPath;

    public JarInfo Info => _info;

    public bool IsDirectory => _info.IsDirectory;

    public override string DisplayName => Path.GetFileName(FullPath.TrimEnd('\\', '/'));

    public override string? Subtitle => Path.GetDirectoryName(FullPath.TrimEnd('\\', '/')) ?? "";

    public override string? Location => FullPath;

    public override ImageMoniker Icon => IsDirectory ? IkvmMonikers.ClassFolder : IkvmMonikers.JarFile;

    public override string? Status => base.Status ?? (_description?.IsResolved == false ? _description.Diagnostic : null);

    /// <summary>
    /// Gets the include the entry is written with, and named with by the entries that reference it.
    /// </summary>
    public string GetInclude() => Original?.Include ?? ToProjectPath(Context.ProjectDirectory, _fullPath, IsDirectory, Context.UseRelativePaths);

    // assembly name and version

    public string AssemblyName
    {
        get => Common(i => i.AssemblyName);
        set => Apply(i => i.AssemblyName = value);
    }

    public string? AssemblyNameVariations => Variations(i => i.AssemblyName);

    public string AssemblyNamePlaceholder => Varies(i => i.AssemblyName) ? VariesPlaceholder : Placeholder(_description?.AssemblyName);

    public string AssemblyVersion
    {
        get => Common(i => i.AssemblyVersion);
        set => Apply(i => i.AssemblyVersion = value);
    }

    public string? AssemblyVersionVariations => Variations(i => i.AssemblyVersion);

    public string AssemblyVersionPlaceholder => Varies(i => i.AssemblyVersion) ? VariesPlaceholder : Placeholder(_description?.AssemblyVersion);

    string Placeholder(string? detected) => IsEditable == false ? "Not set" : _isDescribed == false ? "Detecting..." : detected != null ? $"{detected} (detected)" : "Not detected: set a value";

    // classes and sources

    /// <summary>
    /// The classes compiled into the assembly for the target frameworks being edited, in order.
    /// </summary>
    public IReadOnlyList<PathItem> ClassItems => GetPathItems(i => i.Classes);

    public string? ClassesVariations => Variations(i => Join(i.Classes.Select(Path.GetFileName)));

    /// <summary>
    /// The source archives for the target frameworks being edited, in order.
    /// </summary>
    public IReadOnlyList<PathItem> SourceItems => GetPathItems(i => i.Sources);

    public string? SourcesVariations => Variations(i => Join(i.Sources.Select(Path.GetFileName)));

    static string Join(IEnumerable<string> values) => string.Join(", ", values);

    /// <summary>
    /// Gets the paths of a list for the target frameworks being edited: the first one's order, followed by any the
    /// others add.
    /// </summary>
    List<string> GetPathOrder(Func<JarDependencyValues, List<string>> list)
    {
        return ShownValues.SelectMany(list).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    List<PathItem> GetPathItems(Func<JarDependencyValues, List<string>> list)
    {
        return GetPathOrder(list).Select(path =>
        {
            var keys = ShownKeys.Where(i => list(GetValues(i)).Contains(path, StringComparer.OrdinalIgnoreCase)).ToList();
            return new PathItem(path, keys.Count < ShownKeys.Count ? "Only for " + string.Join(", ", keys) : null);
        }).ToList();
    }

    /// <summary>
    /// Moves paths of a list to a position, for the target frameworks being edited, adding those not yet listed.
    /// </summary>
    void MovePaths(Func<JarDependencyValues, List<string>> list, IReadOnlyList<string> paths, int index)
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

    void RemovePath(Func<JarDependencyValues, List<string>> list, string path)
    {
        Apply(i => list(i).RemoveAll(j => string.Equals(j, path, StringComparison.OrdinalIgnoreCase)));
    }

    public void MoveClass(string path, int index) => MovePaths(i => i.Classes, new[] { path }, index);

    public void RemoveClass(string path) => RemovePath(i => i.Classes, path);

    public void MoveSource(string path, int index) => MovePaths(i => i.Sources, new[] { path }, index);

    public void RemoveSource(string path) => RemovePath(i => i.Sources, path);

    /// <summary>
    /// Adds classes at a position for the target frameworks being edited.
    /// </summary>
    public void AddClasses(IEnumerable<string> paths, int index = int.MaxValue) => MovePaths(i => i.Classes, paths.ToList(), index);

    /// <summary>
    /// Adds sources at a position for the target frameworks being edited.
    /// </summary>
    public void AddSources(IEnumerable<string> paths, int index = int.MaxValue) => MovePaths(i => i.Sources, paths.ToList(), index);

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
    List<JarDependencyEntry> GetReferenceOrder()
    {
        var order = new List<JarDependencyEntry>();
        foreach (var values in ShownValues)
            foreach (var target in values.References)
                if (target.IsRemoved == false && order.Contains(target) == false)
                    order.Add(target);

        return order;
    }

    /// <summary>
    /// Moves a referenced entry to the given position, for the target frameworks being edited; one not yet referenced
    /// is added there.
    /// </summary>
    public void MoveReference(JarDependencyEntry target, int index)
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
    /// Lists the target frameworks being edited in which the entry references the given entry, when that is only some
    /// of them.
    /// </summary>
    public string? GetReferencesNote(JarDependencyEntry target)
    {
        return GetReferences(target) == null ? "Only for " + string.Join(", ", ShownKeys.Where(i => GetValues(i).References.Contains(target))) : null;
    }

    /// <summary>
    /// Gets whether the entry references the given entry: for all target frameworks in view, none, or some (<c>null</c>).
    /// </summary>
    public bool? GetReferences(JarDependencyEntry target)
    {
        var count = ShownValues.Count(i => i.References.Contains(target));
        return count == 0 ? false : count == ShownKeys.Count ? true : null;
    }

    /// <summary>
    /// Sets whether the entry references the given entry, for the target frameworks in view.
    /// </summary>
    public void SetReferences(JarDependencyEntry target, bool value)
    {
        Apply(i =>
        {
            if (value == false)
                i.References.Remove(target);
            else if (i.References.Contains(target) == false)
                i.References.Add(target);
        });
    }

    /// <summary>
    /// The other JAR entries in the dialog.
    /// </summary>
    IEnumerable<JarDependencyEntry> Others => Context.Entries.OfType<JarDependencyEntry>().Where(i => i != this);

    protected internal override void OnEntriesChanged()
    {
        // the entries named by References metadata, once they are all listed
        if (_referencesResolved == false && Original != null)
        {
            foreach (var key in Keys)
                foreach (var name in Split(GetOriginalMetadata(ReferencesKey, key)))
                    if (FindReferenced(name) is { } target && GetValues(key).References.Contains(target) == false)
                        GetValues(key).References.Add(target);
        }

        _referencesResolved = true;

        Dependencies.Clear();
        foreach (var other in Others.Where(i => i.IsRemoved == false))
            Dependencies.Add(new DependencyOption(this, other));

        Refresh();
    }

    /// <summary>
    /// Finds the entry named in References metadata.
    /// </summary>
    JarDependencyEntry? FindReferenced(string name)
    {
        var fullPath = Resolve(Context.ProjectDirectory, name).TrimEnd('\\');
        return Others.FirstOrDefault(i =>
            string.Equals(i.Original?.Include, name, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(i.DescribedPath.TrimEnd('\\'), fullPath, StringComparison.OrdinalIgnoreCase));
    }

    protected override void OnRefresh()
    {
        _info = JarInfo.Read(FullPath);

        foreach (var option in Dependencies)
            option.Refresh();
    }

    // validation

    HashSet<JarDependencyEntry> _cycleTargets = new HashSet<JarDependencyEntry>();

    public FieldState CompileFrame => GetFieldState(IsInvalid(i => i.Classes.Count > 0), Varies(i => Join(i.Classes)));

    public FieldState SourcesFrame => GetFieldState(false, Varies(i => Join(i.Sources)));

    public FieldState AssemblyNameFrame => GetFieldState(IsInvalid(i => IsAssemblyNameValid(i.AssemblyName)), Varies(i => i.AssemblyName));

    public FieldState AssemblyVersionFrame => GetFieldState(IsInvalid(i => IsAssemblyVersionValid(i.AssemblyVersion)), Varies(i => i.AssemblyVersion));

    public FieldState DependenciesFrame => GetFieldState(_cycleTargets.Count > 0, Dependencies.Any(i => i.IsChecked == null));

    static bool IsAssemblyNameValid(string value) => value.Trim().IndexOfAny(Path.GetInvalidFileNameChars()) < 0;

    static bool IsAssemblyVersionValid(string value) => string.IsNullOrWhiteSpace(value) || Version.TryParse(value.Trim(), out _);

    /// <summary>
    /// Whether depending on the given entry leads back to this one.
    /// </summary>
    public bool IsCycleThrough(JarDependencyEntry target) => _cycleTargets.Contains(target);

    /// <summary>
    /// The entries this one references for a target framework.
    /// </summary>
    IEnumerable<JarDependencyEntry> GetReferencedEntries(string key) => GetValues(key).References.Where(i => i.IsRemoved == false);

    protected override void Validate(IkvmDependencyValidation validation)
    {
        AddError(validation, i => i.Classes.Count > 0, "Compile is empty: add a JAR or class folder to compile.");
        AddError(validation, i => IsAssemblyNameValid(i.AssemblyName), "The assembly name contains characters a file name cannot.");
        AddError(validation, i => IsAssemblyVersionValid(i.AssemblyVersion), "The assembly version is not a version, such as 1.2.3.4.");

        var cycles = FindCycles();
        foreach (var cycle in cycles.GroupBy(i => i.Path))
            validation.AddError(cycle.Select(i => i.Key), $"Depends on itself: {cycle.Key}.");

        _cycleTargets = new HashSet<JarDependencyEntry>(cycles.Select(i => i.Through));

        foreach (var option in Dependencies)
            option.Refresh();
    }

    /// <summary>
    /// Finds, for each target framework the entry is used in, a path through "Depends on" that leads back to it.
    /// </summary>
    List<(string Key, string Path, JarDependencyEntry Through)> FindCycles()
    {
        var cycles = new List<(string Key, string Path, JarDependencyEntry Through)>();

        foreach (var key in UsedKeys)
        {
            var path = new List<JarDependencyEntry>() { this };
            var visited = new HashSet<JarDependencyEntry>();
            if (Visit(this))
                cycles.Add((key, string.Join(" → ", path.Select(i => i.DisplayName)), path[1]));

            bool Visit(JarDependencyEntry from)
            {
                foreach (var next in from.GetReferencedEntries(key))
                {
                    if (next == this)
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

    // description

    /// <summary>
    /// What the project's IKVM package derives for this entry, once known.
    /// </summary>
    public IkvmReferenceDescription? Description
    {
        get => _description;
        set
        {
            _description = value;
            _isDescribed = true;
            OnPropertyChanged(string.Empty);
        }
    }

    // saving

    /// <summary>
    /// Writes the include and metadata. Values the same for every target framework the entry applies to are written
    /// for all of them; values that differ are written for each.
    /// </summary>
    protected override void Save(IkvmDependencyElementBuilder builder)
    {
        var useRelativePaths = Context.UseRelativePaths;
        builder.Include = GetInclude();

        SetMetadata(builder, AssemblyNameKey, i => i.AssemblyName.Trim());
        SetMetadata(builder, AssemblyVersionKey, i => i.AssemblyVersion.Trim());
        // Compile is left unset when it would list only the entry itself, its default
        SetMetadata(builder, CompileKey, i => i.Classes.Count == 0 || (i.Classes.Count == 1 && string.Equals(i.Classes[0], _fullPath, StringComparison.OrdinalIgnoreCase)) ? "" : string.Join(";", i.Classes.Select(j => ToProjectPath(Context.ProjectDirectory, j, Directory.Exists(j), useRelativePaths))));
        SetMetadata(builder, SourcesKey, i => string.Join(";", i.Sources.Select(j => ToProjectPath(Context.ProjectDirectory, j, false, useRelativePaths))));
        SetMetadata(builder, ReferencesKey, i => string.Join(";", i.References.Where(j => j.IsRemoved == false).Select(j => j.GetInclude())));
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
