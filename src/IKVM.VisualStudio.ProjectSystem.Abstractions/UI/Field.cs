using System.Windows;
using System.Windows.Controls;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// Hosts one input control and draws its only border: focused, varying between target frameworks, or not valid. The
/// hosted control draws no border of its own.
/// </summary>
public sealed class Field : ContentControl
{

    public static readonly DependencyProperty StateProperty = DependencyProperty.Register(nameof(State), typeof(FieldState), typeof(Field), new PropertyMetadata(FieldState.Normal));

    /// <summary>
    /// The state the border shows.
    /// </summary>
    public FieldState State
    {
        get => (FieldState)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

}
