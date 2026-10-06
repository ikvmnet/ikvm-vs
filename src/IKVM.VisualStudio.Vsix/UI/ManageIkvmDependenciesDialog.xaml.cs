using System;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;

using IKVM.VisualStudio.ProjectSystem.UI;
using IKVM.VisualStudio.Vsix.ProjectSystem.References;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.PlatformUI;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// Edits the IKVM dependencies of a project: the items already in it and new ones, saved together.
/// </summary>
internal partial class ManageIkvmDependenciesDialog : DialogWindow
{

    readonly ManageIkvmDependenciesViewModel _model;
    IkvmDependencyEntry? _pressedEntry;
    Point _pressedAt;

    public ManageIkvmDependenciesDialog(IkvmDependencySession session)
    {
        InitializeComponent();
        DataContext = _model = new ManageIkvmDependenciesViewModel(session);

        // existing entries first, then new ones
        var view = CollectionViewSource.GetDefaultView(_model.Entries);
        view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(IkvmDependencyEntry.GroupName)));
        view.SortDescriptions.Add(new SortDescription(nameof(IkvmDependencyEntry.GroupOrder), ListSortDirection.Ascending));
    }

    /// <summary>
    /// Gets the changes to save.
    /// </summary>
    public IkvmDependencyChanges GetChanges() => _model.GetChanges();

    async void OnAdd(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: IkvmDependencyAddCommand command })
            await _model.AddAsync(command, this);
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

    /// <summary>
    /// Shows the context menu of an entry: the items of the entry, then those of the dialog.
    /// </summary>
    void OnEntryContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source || ListReorder.FindAncestor<ListBoxItem>(source) is not { DataContext: IkvmDependencyEntry entry })
        {
            e.Handled = true;
            return;
        }

        _model.SelectedEntry = entry;

        var menu = EntryList.ContextMenu;
        menu.Items.Clear();
        foreach (var menuItem in entry.GetMenuItems())
        {
            var control = new MenuItem() { Header = menuItem.Text, IsEnabled = menuItem.IsEnabled };
            if (menuItem.Icon.Guid != Guid.Empty)
                control.Icon = new CrispImage() { Moniker = menuItem.Icon, Width = 16, Height = 16 };

            control.Click += (s, a) => menuItem.Execute();
            menu.Items.Add(control);
        }

        if (menu.Items.Count > 0)
            menu.Items.Add(new Separator());

        var remove = new MenuItem() { Header = entry.IsRemoved ? "_Undo Remove" : "_Remove", IsEnabled = entry.IsEditable, InputGestureText = "Del" };
        remove.Click += (s, a) => _model.ToggleRemove(entry);
        menu.Items.Add(remove);
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

    // entries in the list are selected when the mouse is released, so that dragging one into a view, such as
    // "Depends on", leaves the selection, and the details shown, as they are

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
            _model.AddPaths(paths);
    }

    void OnSave(object sender, RoutedEventArgs e)
    {
        if (_model.CanSave == false)
            return;

        DialogResult = true;
    }

}
