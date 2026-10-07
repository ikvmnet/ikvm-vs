using System;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// A target framework on an entry of the Manage IKVM Dependencies dialog, or all of them: whether the entry is used in
/// it (its check box), and whether its settings are being viewed and edited (its name, filled while editing).
/// </summary>
public sealed class TargetFrameworkChip : ViewModelBase
{

    readonly Action<TargetFrameworkChip> _toggleUsed;
    readonly Action<TargetFrameworkChip> _toggleEditing;
    bool _isUsed;
    bool _isEditing;
    bool _canChangeUsed;

    /// <summary>
    /// Initializes a chip for one target framework, or for all of them, that hands clicks back to its entry.
    /// </summary>
    /// <param name="name">Name of the target framework, or the text of the chip standing for all of them.</param>
    /// <param name="isAll">Whether the chip stands for every target framework.</param>
    /// <param name="toggleUsed">Called when the check box is clicked, to add the entry to or take it out of the target framework.</param>
    /// <param name="toggleEditing">Called when the name is clicked, to start or stop editing the target framework.</param>
    public TargetFrameworkChip(string name, bool isAll, Action<TargetFrameworkChip> toggleUsed, Action<TargetFrameworkChip> toggleEditing)
    {
        Name = name;
        IsAll = isAll;
        _toggleUsed = toggleUsed;
        _toggleEditing = toggleEditing;
    }

    /// <summary>
    /// Gets the target framework the chip stands for, also shown as its name; <c>All</c> for the chip standing for
    /// every one.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether the chip stands for every target framework.
    /// </summary>
    public bool IsAll { get; }

    /// <summary>
    /// Whether the entry is used in the target framework, shown by the check box. Set by the entry.
    /// </summary>
    public bool IsUsed
    {
        get => _isUsed;
        set
        {
            if (Set(ref _isUsed, value))
                OnPropertyChanged(nameof(UsedToolTip));
        }
    }

    /// <summary>
    /// Whether the target framework's settings are being viewed and edited, shown by filling the name. Set by the
    /// entry.
    /// </summary>
    public bool IsEditing
    {
        get => _isEditing;
        set
        {
            if (Set(ref _isEditing, value))
                OnPropertyChanged(nameof(EditingToolTip));
        }
    }

    /// <summary>
    /// Whether the entry can be added to or taken out of the target framework.
    /// </summary>
    public bool CanChangeUsed
    {
        get => _canChangeUsed;
        set => Set(ref _canChangeUsed, value);
    }

    /// <summary>
    /// Gets the tool tip of the check box: whether the entry is used in the target framework, and what clicking does.
    /// </summary>
    public string UsedToolTip => IsAll
        ? (_isUsed ? "Used in every target framework, including ones added later. Click to list them instead." : "Use in every target framework, including ones added later.")
        : (_isUsed ? $"Used in {Name}. Click to stop using it there." : $"Not used in {Name}. Click to use it there.");

    /// <summary>
    /// Gets the tool tip of the name: whether the target framework is being edited, and what clicking does.
    /// </summary>
    public string EditingToolTip => IsAll
        ? (_isEditing ? "Editing every target framework. Click to stop." : "View and edit every target framework.")
        : (_isEditing ? $"Editing {Name}. Click to stop." : $"View and edit {Name}.");

    /// <summary>
    /// Asks the entry to add itself to or take itself out of the target framework, when the check box is clicked.
    /// </summary>
    public void ToggleUsed() => _toggleUsed(this);

    /// <summary>
    /// Asks the entry to start or stop editing the target framework, when the name is clicked.
    /// </summary>
    public void ToggleEditing() => _toggleEditing(this);

}
