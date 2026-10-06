namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// A target framework an entry can be limited to.
/// </summary>
sealed class TargetFrameworkOption : ViewModelBase
{

    bool _isChecked;

    public TargetFrameworkOption(string name, bool isChecked)
    {
        Name = name;
        _isChecked = isChecked;
    }

    public string Name { get; }

    public bool IsChecked
    {
        get => _isChecked;
        set => Set(ref _isChecked, value);
    }

}
