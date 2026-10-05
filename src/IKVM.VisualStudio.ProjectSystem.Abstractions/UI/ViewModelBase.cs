using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// Base of the view models of the dialogs, raising <see cref="INotifyPropertyChanged.PropertyChanged"/> for bindings.
/// </summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{

    /// <summary>
    /// Raised when a property changes, or with an empty name when they all may have.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Stores a new value in a property's backing field and raises <see cref="PropertyChanged"/>, unless the value is
    /// unchanged.
    /// </summary>
    /// <param name="field">The backing field.</param>
    /// <param name="value">The new value.</param>
    /// <param name="name">The property's name, by default the caller's.</param>
    /// <returns>Whether the value changed.</returns>
    protected bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(name);
        return true;
    }

    /// <summary>
    /// Raises <see cref="PropertyChanged"/> for the named property, by default the caller; an empty name refreshes
    /// every binding.
    /// </summary>
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

}
