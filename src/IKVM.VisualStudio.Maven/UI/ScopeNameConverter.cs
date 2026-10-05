using System;
using System.Globalization;
using System.Windows.Data;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// Shows the default scope, written as no scope, by its name.
/// </summary>
sealed class ScopeNameConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is string { Length: > 0 } scope ? scope : "compile (default)";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

}
