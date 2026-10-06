using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// Lets the items of a list be reordered by dragging them, or with Alt+Up and Alt+Down, and lets other things be
/// dropped into it at a position. A line marks where a drop would land.
/// </summary>
sealed class ListReorder
{

    const string Format = "IKVM.VisualStudio.ListReorderItem";

    readonly ListBox _list;
    readonly Func<bool> _canEdit;
    readonly Func<IDataObject, object?> _getExternal;
    readonly Action<object, int> _move;
    IReorderableItem? _pressed;
    Point _pressedAt;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="list">The list.</param>
    /// <param name="canEdit">Whether the list can be changed now.</param>
    /// <param name="getExternal">Gets what dropped data from elsewhere would add, or <c>null</c> to refuse it.</param>
    /// <param name="move">Moves one of the list's items, or adds what <paramref name="getExternal"/> returned, to a position in the list.</param>
    public ListReorder(ListBox list, Func<bool> canEdit, Func<IDataObject, object?> getExternal, Action<object, int> move)
    {
        _list = list;
        _canEdit = canEdit;
        _getExternal = getExternal;
        _move = move;

        list.AllowDrop = true;
        list.PreviewMouseLeftButtonDown += OnMouseDown;
        list.PreviewMouseMove += OnMouseMove;
        list.DragEnter += OnDragOver;
        list.DragOver += OnDragOver;
        list.DragLeave += (s, e) => ClearMarkers();
        list.Drop += OnDrop;
        list.KeyDown += OnKeyDown;
    }

    void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _pressed = null;
        if (e.OriginalSource is DependencyObject source && FindAncestor<ButtonBase>(source) == null && FindAncestor<ListBoxItem>(source) is { DataContext: IReorderableItem { CanMove: true } item })
        {
            _pressed = item;
            _pressedAt = e.GetPosition(_list);
        }
    }

    void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (_pressed is not { } item || e.LeftButton != MouseButtonState.Pressed || _canEdit() == false)
            return;

        var moved = e.GetPosition(_list) - _pressedAt;
        if (Math.Abs(moved.X) < SystemParameters.MinimumHorizontalDragDistance && Math.Abs(moved.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        _pressed = null;
        DragDrop.DoDragDrop(_list, new DataObject(Format, item), DragDropEffects.Move);
        ClearMarkers();
    }

    /// <summary>
    /// Gets what a drop would move or add: one of the list's own items, or something from elsewhere.
    /// </summary>
    object? GetDropped(IDataObject data, out bool isOwn)
    {
        isOwn = false;
        if (_canEdit() == false)
            return null;

        if (data.GetDataPresent(Format))
        {
            var item = data.GetData(Format) as IReorderableItem;
            isOwn = item != null && _list.Items.Contains(item);
            return isOwn ? item : null;
        }

        return _getExternal(data);
    }

    /// <summary>
    /// Gets the position a drop would land at, before or after the item under the mouse, else at the end; never
    /// ahead of items that cannot move. Marks it when asked.
    /// </summary>
    int GetDropIndex(DragEventArgs e, bool mark)
    {
        ClearMarkers();

        var items = _list.Items.OfType<IReorderableItem>().ToList();
        var first = items.TakeWhile(i => i.CanMove == false).Count();

        var index = items.Count;
        if (e.OriginalSource is DependencyObject source && FindAncestor<ListBoxItem>(source) is { DataContext: IReorderableItem over } container)
            index = items.IndexOf(over) + (e.GetPosition(container).Y > container.ActualHeight / 2 ? 1 : 0);

        index = Math.Max(index, first);

        if (mark && items.Count > 0)
        {
            if (index < items.Count)
                items[index].IsDropBefore = true;
            else
                items[items.Count - 1].IsDropAfter = true;
        }

        return index;
    }

    void ClearMarkers()
    {
        foreach (var item in _list.Items.OfType<IReorderableItem>())
            item.IsDropBefore = item.IsDropAfter = false;
    }

    void OnDragOver(object sender, DragEventArgs e)
    {
        if (GetDropped(e.Data, out var isOwn) != null)
        {
            e.Effects = isOwn ? DragDropEffects.Move : (e.AllowedEffects & DragDropEffects.Link) != 0 ? DragDropEffects.Link : DragDropEffects.Copy;
            GetDropIndex(e, mark: true);
        }
        else
        {
            e.Effects = DragDropEffects.None;
        }

        e.Handled = true;
    }

    void OnDrop(object sender, DragEventArgs e)
    {
        if (GetDropped(e.Data, out var isOwn) is { } dropped)
        {
            var index = GetDropIndex(e, mark: false);

            // moving an item down: the position counts it where it was
            if (isOwn && _list.Items.IndexOf(dropped) is var current && current >= 0 && current < index)
                index--;

            Move(dropped, index);
        }

        ClearMarkers();
        e.Handled = true;
    }

    void OnKeyDown(object sender, KeyEventArgs e)
    {
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if ((Keyboard.Modifiers & ModifierKeys.Alt) == 0 || (key != Key.Up && key != Key.Down))
            return;

        if (_canEdit() && _list.SelectedItem is IReorderableItem { CanMove: true } item)
        {
            var items = _list.Items.OfType<IReorderableItem>().ToList();
            var first = items.TakeWhile(i => i.CanMove == false).Count();
            var index = items.IndexOf(item) + (key == Key.Up ? -1 : 1);
            if (index >= first && index < items.Count)
                Move(item, index);
        }

        e.Handled = true;
    }

    /// <summary>
    /// Moves, then selects and focuses the moved item again in the rebuilt list.
    /// </summary>
    void Move(object dropped, int index)
    {
        var key = (dropped as IReorderableItem)?.Key;
        _move(dropped, index);

        if (key != null && _list.Items.OfType<IReorderableItem>().FirstOrDefault(i => Equals(i.Key, key)) is { } moved)
        {
            _list.SelectedItem = moved;
            _list.UpdateLayout();
            (_list.ItemContainerGenerator.ContainerFromItem(moved) as UIElement)?.Focus();
        }
    }

    public static T? FindAncestor<T>(DependencyObject element) where T : DependencyObject
    {
        for (var i = element; i != null; i = i is Visual ? VisualTreeHelper.GetParent(i) : LogicalTreeHelper.GetParent(i))
            if (i is T found)
                return found;

        return null;
    }

}
