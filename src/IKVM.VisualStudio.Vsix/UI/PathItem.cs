using System.IO;

using IKVM.VisualStudio.ProjectSystem.UI;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// A path in the Classes or Sources list of the Manage IKVM Dependencies dialog.
/// </summary>
sealed class PathItem : ViewModelBase, IReorderableItem
{

    bool _isDropBefore;
    bool _isDropAfter;

    /// <summary>
    /// Creates the item for a full path, with the note of the target frameworks it applies to when that is only some.
    /// </summary>
    public PathItem(string path, string? partialNote)
    {
        Path = path;
        PartialNote = partialNote;
        IsDirectory = Directory.Exists(path);
    }

    /// <summary>
    /// The full path.
    /// </summary>
    public string Path { get; }

    /// <summary>
    /// Whether the path was an existing directory when the item was created.
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    /// The target frameworks the path applies to, when that is only some of those being edited.
    /// </summary>
    public string? PartialNote { get; }

    /// <summary>
    /// Whether the path applies to only some of the target frameworks being edited.
    /// </summary>
    public bool IsPartial => PartialNote != null;

    /// <summary>
    /// The path, followed by the partial note when there is one.
    /// </summary>
    public string ToolTip => PartialNote != null ? $"{Path}\n{PartialNote}" : Path;

    /// <summary>
    /// The path, ignoring case.
    /// </summary>
    public object Key => Path.ToUpperInvariant();

    /// <inheritdoc />
    public bool CanMove => true;

    /// <inheritdoc />
    public bool IsDropBefore
    {
        get => _isDropBefore;
        set => Set(ref _isDropBefore, value);
    }

    /// <inheritdoc />
    public bool IsDropAfter
    {
        get => _isDropAfter;
        set => Set(ref _isDropAfter, value);
    }

}
