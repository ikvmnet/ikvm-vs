using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// Shows an element while a value is <c>false</c>.
/// </summary>
public sealed class FalseToVisibleConverter : IValueConverter
{

    /// <summary>
    /// Returns <see cref="Visibility.Visible"/> when <paramref name="value"/> is <c>false</c>, else
    /// <see cref="Visibility.Collapsed"/>, including for <c>null</c> and values that are not booleans.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is false ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Not supported: the converter only works one way.
    /// </summary>
    /// <exception cref="NotSupportedException">Always.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

}
