using System;
using System.Globalization;
using System.Windows.Data;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// Shows the default scope, written as no scope, by its name.
/// </summary>
sealed class ScopeNameConverter : IValueConverter
{

    /// <summary>
    /// Shows a scope as itself, and the empty scope as <c>compile (default)</c>.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is string { Length: > 0 } scope ? scope : "compile (default)";

    /// <summary>
    /// Not supported: scope names are only shown.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotSupportedException();

}
