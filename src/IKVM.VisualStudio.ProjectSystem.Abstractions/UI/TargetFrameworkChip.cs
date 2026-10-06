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

    public TargetFrameworkChip(string name, bool isAll, Action<TargetFrameworkChip> toggleUsed, Action<TargetFrameworkChip> toggleEditing)
    {
        Name = name;
        IsAll = isAll;
        _toggleUsed = toggleUsed;
        _toggleEditing = toggleEditing;
    }

    public string Name { get; }

    /// <summary>
    /// Whether the chip stands for every target framework.
    /// </summary>
    public bool IsAll { get; }

    public bool IsUsed
    {
        get => _isUsed;
        set
        {
            if (Set(ref _isUsed, value))
                OnPropertyChanged(nameof(UsedToolTip));
        }
    }

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

    public string UsedToolTip => IsAll
        ? (_isUsed ? "Used in every target framework, including ones added later. Click to list them instead." : "Use in every target framework, including ones added later.")
        : (_isUsed ? $"Used in {Name}. Click to stop using it there." : $"Not used in {Name}. Click to use it there.");

    public string EditingToolTip => IsAll
        ? (_isEditing ? "Editing every target framework. Click to stop." : "View and edit every target framework.")
        : (_isEditing ? $"Editing {Name}. Click to stop." : $"View and edit {Name}.");

    public void ToggleUsed() => _toggleUsed(this);

    public void ToggleEditing() => _toggleEditing(this);

}
