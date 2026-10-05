namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// A target framework an entry can be limited to.
/// </summary>
public sealed class TargetFrameworkOption : ViewModelBase
{

    bool _isChecked;

    /// <summary>
    /// Initializes an option for the named target framework, checked if the entry starts out limited to it.
    /// </summary>
    public TargetFrameworkOption(string name, bool isChecked)
    {
        Name = name;
        _isChecked = isChecked;
    }

    /// <summary>
    /// Gets the name of the target framework, such as <c>net8.0</c>.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Whether the entry is used in the target framework.
    /// </summary>
    public bool IsChecked
    {
        get => _isChecked;
        set => Set(ref _isChecked, value);
    }

}
