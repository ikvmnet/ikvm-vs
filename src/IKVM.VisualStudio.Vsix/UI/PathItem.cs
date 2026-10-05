using System.IO;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// A path in the Classes or Sources list of the Manage IKVM Dependencies dialog.
/// </summary>
sealed class PathItem : ViewModelBase, IReorderableItem
{

    bool _isDropBefore;
    bool _isDropAfter;

    public PathItem(string path, string? partialNote)
    {
        Path = path;
        PartialNote = partialNote;
        IsDirectory = Directory.Exists(path);
    }

    public string Path { get; }

    public bool IsDirectory { get; }

    /// <summary>
    /// The target frameworks the path applies to, when that is only some of those being edited.
    /// </summary>
    public string? PartialNote { get; }

    public bool IsPartial => PartialNote != null;

    public string ToolTip => PartialNote != null ? $"{Path}\n{PartialNote}" : Path;

    public object Key => Path.ToUpperInvariant();

    public bool CanMove => true;

    public bool IsDropBefore
    {
        get => _isDropBefore;
        set => Set(ref _isDropBefore, value);
    }

    public bool IsDropAfter
    {
        get => _isDropAfter;
        set => Set(ref _isDropAfter, value);
    }

}
