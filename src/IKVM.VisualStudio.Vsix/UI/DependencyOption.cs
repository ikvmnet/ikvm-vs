using IKVM.VisualStudio.ProjectSystem.UI;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// Another entry that an entry may depend on, through its <c>References</c> metadata. Checked when the entry
/// references it for every target framework in view, unchecked for none, and indeterminate otherwise.
/// </summary>
sealed class DependencyOption : ViewModelBase, IReorderableItem
{

    readonly JarDependencyEntry _owner;
    bool _isDropBefore;
    bool _isDropAfter;

    /// <summary>
    /// Creates the option for <paramref name="owner"/> to depend on <paramref name="target"/>.
    /// </summary>
    public DependencyOption(JarDependencyEntry owner, JarDependencyEntry target)
    {
        _owner = owner;
        Target = target;
    }

    /// <summary>
    /// The entry that may be depended on.
    /// </summary>
    public JarDependencyEntry Target { get; }

    /// <summary>
    /// The name shown for the target.
    /// </summary>
    public string DisplayName => Target.DisplayName;

    /// <inheritdoc />
    public object Key => Target;

    /// <inheritdoc />
    public bool CanMove => true;

    /// <summary>
    /// Whether the owner references the target: for every target framework in view, for none, or
    /// <see langword="null"/> for some. Setting it adds or removes the reference for all of them.
    /// </summary>
    public bool? IsChecked
    {
        get => _owner.GetReferences(Target);
        set
        {
            _owner.SetReferences(Target, value == true);
            Refresh();
        }
    }

    /// <summary>
    /// Why the dependency is invalid, or the target frameworks it applies to when that is only some of those being
    /// edited.
    /// </summary>
    public string? ToolTip => IsInvalid ? $"{Target.DisplayName} depends, directly or not, on {_owner.DisplayName}." : _owner.GetReferencesNote(Target);

    /// <summary>
    /// Whether depending on the target leads back to the owner.
    /// </summary>
    public bool IsInvalid => _owner.IsCycleThrough(Target);

    /// <summary>
    /// Whether an item being dragged would land before this one.
    /// </summary>
    public bool IsDropBefore
    {
        get => _isDropBefore;
        set => Set(ref _isDropBefore, value);
    }

    /// <summary>
    /// Whether an item being dragged would land after this one.
    /// </summary>
    public bool IsDropAfter
    {
        get => _isDropAfter;
        set => Set(ref _isDropAfter, value);
    }

    /// <summary>
    /// Raises change notifications for the values derived from the owner's references, after they may have changed.
    /// </summary>
    public void Refresh()
    {
        OnPropertyChanged(nameof(IsChecked));
        OnPropertyChanged(nameof(ToolTip));
        OnPropertyChanged(nameof(IsInvalid));
    }

}
