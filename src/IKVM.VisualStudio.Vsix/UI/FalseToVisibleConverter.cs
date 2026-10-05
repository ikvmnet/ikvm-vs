using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// Shows an element while a value is <c>false</c>.
/// </summary>
sealed class FalseToVisibleConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is false ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

}
