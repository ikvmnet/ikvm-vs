using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Input;

using IKVM.VisualStudio.Vsix.ProjectSystem.References;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.Win32;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// Edits a project's IKVM dependencies: the references already in it and new ones, saved together.
/// </summary>
internal partial class ManageIkvmDependenciesDialog : DialogWindow
{

    readonly ManageIkvmDependenciesViewModel _model;
    readonly string _projectDirectory;
    IkvmDependencyEntry? _pressedEntry;
    Point _pressedAt;

    public ManageIkvmDependenciesDialog(
        string projectDirectory,
        IEnumerable<IkvmReferenceElement> elements,
        IEnumerable<string> targetFrameworks,
        string? defaultTargetFramework,
        Func<IReadOnlyCollection<string>, CancellationToken, Task<IReadOnlyDictionary<string, IkvmReferenceDescription>>> describe)
    {
        InitializeComponent();
        _projectDirectory = projectDirectory;
        DataContext = _model = new ManageIkvmDependenciesViewModel(projectDirectory, elements, targetFrameworks, defaultTargetFramework, describe);

        // existing references first, then new ones
        // the ordered lists: reordered by dragging, and taking drops at a position
        new ListReorder(ClassList, CanEditSelected, GetDroppedFiles, (dropped, index) =>
        {
            if (dropped is PathItem item)
                _model.SelectedEntry?.MoveClass(item.Path, index);
            else if (dropped is string[] paths)
                _model.SelectedEntry?.AddClasses(paths, index);
        });
        new ListReorder(SourceList, CanEditSelected, GetDroppedFiles, (dropped, index) =>
        {
            if (dropped is PathItem item)
                _model.SelectedEntry?.MoveSource(item.Path, index);
            else if (dropped is string[] paths)
                _model.SelectedEntry?.AddSources(paths, index);
        });
        new ListReorder(DependencyList, CanEditSelected, GetDroppedReference, (dropped, index) =>
        {
            if (dropped is DependencyOption option)
                _model.SelectedEntry?.MoveReference(option.Target, index);
            else if (dropped is IkvmDependencyEntry target)
                _model.SelectedEntry?.MoveReference(target, index);
        });

        var view = CollectionViewSource.GetDefaultView(_model.Entries);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(IkvmDependencyEntry.GroupName)));
        view.SortDescriptions.Add(new SortDescription(nameof(IkvmDependencyEntry.GroupOrder), ListSortDirection.Ascending));
    }

    /// <summary>
    /// Gets the changes to save.
    /// </summary>
    public IkvmReferenceChanges GetChanges() => _model.GetChanges();

    void OnAddJars(object sender, RoutedEventArgs e)
    {
        var paths = PickFiles("Add JAR Files", "Java archives (*.jar)|*.jar|All files (*.*)|*.*");
        if (paths != null)
            _model.AddPaths(paths);
    }

    void OnAddFolder(object sender, RoutedEventArgs e)
    {
        var path = PickFolder("Select a folder of .class files");
        if (path != null)
            _model.AddPaths(new[] { path });
    }

    void OnRemoveItem(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IkvmDependencyEntry entry })
            _model.ToggleRemove(entry);
    }

    void OnEntryListKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _model.SelectedEntry is { } entry)
        {
            _model.ToggleRemove(entry);
            e.Handled = true;
        }
    }

    void OnChipUsedClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TargetFrameworkChip chip })
            chip.ToggleUsed();
    }

    void OnChipEditingClick(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TargetFrameworkChip chip })
            chip.ToggleEditing();
    }

    void OnAddDependency(object sender, RoutedEventArgs e)
    {
        if (_model.SelectedEntry is not { } entry || sender is not FrameworkElement button)
            return;

        var menu = new ContextMenu() { PlacementTarget = button, Placement = PlacementMode.Bottom };
        foreach (var option in entry.AddableDependencies)
        {
            var item = new MenuItem() { Header = option, HeaderTemplate = (DataTemplate)FindResource("DependencyMenuHeader") };
            item.Click += (s, a) => option.IsChecked = true;
            menu.Items.Add(item);
        }

        if (menu.Items.Count == 0)
            menu.Items.Add(new MenuItem() { Header = "No other references", IsEnabled = false });

        menu.IsOpen = true;
    }

    bool CanEditSelected() => _model.SelectedEntry is { CanEdit: true };

    /// <summary>
    /// Gets files dropped from elsewhere, for Classes and Sources.
    /// </summary>
    static object? GetDroppedFiles(IDataObject data)
    {
        return data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths ? paths : null;
    }

    /// <summary>
    /// Gets a reference dragged from the list, for "Depends on", if the selected entry can depend on it.
    /// </summary>
    object? GetDroppedReference(IDataObject data)
    {
        return data.GetData(typeof(IkvmDependencyEntry)) is IkvmDependencyEntry target && _model.SelectedEntry is { } entry && entry.Dependencies.Any(i => i.Target == target) ? target : null;
    }

    void OnRemoveClass(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PathItem item })
            _model.SelectedEntry?.RemoveClass(item.Path);
    }

    void OnRemoveSource(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PathItem item })
            _model.SelectedEntry?.RemoveSource(item.Path);
    }

    void OnRemoveDependency(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DependencyOption option })
            option.IsChecked = false;
    }

    void OnAddClasses(object sender, RoutedEventArgs e)
    {
        if (_model.SelectedEntry is not { } entry)
            return;

        entry.AddClasses(PickFiles("Add to Compile", "Java archives (*.jar)|*.jar|All files (*.*)|*.*") ?? Enumerable.Empty<string>());
    }

    void OnAddSources(object sender, RoutedEventArgs e)
    {
        if (_model.SelectedEntry is not { } entry)
            return;

        entry.AddSources(PickFiles("Add Sources", "Source archives (*.jar;*.zip)|*.jar;*.zip|All files (*.*)|*.*") ?? Enumerable.Empty<string>());
    }

    // entries in the list are selected when the mouse is released, so that dragging one into "Depends on" leaves the
    // selection, and the details shown, as they are

    void OnEntryListMouseDown(object sender, MouseButtonEventArgs e)
    {
        _pressedEntry = null;
        if (e.OriginalSource is not DependencyObject source || ListReorder.FindAncestor<ButtonBase>(source) != null)
            return;

        if (ListReorder.FindAncestor<ListBoxItem>(source) is { DataContext: IkvmDependencyEntry entry })
        {
            _pressedEntry = entry;
            _pressedAt = e.GetPosition(EntryList);
            EntryList.Focus();
            e.Handled = true;
        }
    }

    void OnEntryListMouseMove(object sender, MouseEventArgs e)
    {
        if (_pressedEntry is not { } entry || e.LeftButton != MouseButtonState.Pressed)
            return;

        var moved = e.GetPosition(EntryList) - _pressedAt;
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _pressedEntry = null;
        DragDrop.DoDragDrop(EntryList, new DataObject(typeof(IkvmDependencyEntry), entry), DragDropEffects.Link);
    }

    void OnEntryListMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_pressedEntry is { } entry)
            _model.SelectedEntry = entry;

        _pressedEntry = null;
    }

    void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is string[] paths)
            _model.AddPaths(paths.Where(i => Directory.Exists(i) || string.Equals(Path.GetExtension(i), ".jar", StringComparison.OrdinalIgnoreCase)));
    }

    void OnSave(object sender, RoutedEventArgs e)
    {
        if (_model.CanSave == false)
            return;

        DialogResult = true;
    }

    string[]? PickFiles(string title, string filter)
    {
        var dialog = new OpenFileDialog() { Title = title, Filter = filter, Multiselect = true, InitialDirectory = _projectDirectory };
        return dialog.ShowDialog(this) == true ? dialog.FileNames : null;
    }

    string? PickFolder(string description)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog() { Description = description, SelectedPath = _projectDirectory, ShowNewFolderButton = false };
        return dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK ? dialog.SelectedPath : null;
    }

}
