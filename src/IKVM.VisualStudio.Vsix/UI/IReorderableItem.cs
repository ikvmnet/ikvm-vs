namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// An item of a list whose order can be changed by dragging.
/// </summary>
interface IReorderableItem
{

    /// <summary>
    /// Identifies the item across rebuilds of the list.
    /// </summary>
    object Key { get; }

    /// <summary>
    /// Whether the item can be moved. Items that cannot stay ahead of those that can.
    /// </summary>
    bool CanMove { get; }

    /// <summary>
    /// Whether an item being dragged would land before this one.
    /// </summary>
    bool IsDropBefore { get; set; }

    /// <summary>
    /// Whether an item being dragged would land after this one.
    /// </summary>
    bool IsDropAfter { get; set; }

}
