using System;
using System.Globalization;
using System.Windows.Data;

using IKVM.VisualStudio.Vsix.Imaging;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// Converts whether a IKVM reference is a class directory into the image moniker for it.
/// </summary>
sealed class ReferenceKindMonikerConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is true ? IkvmMonikers.ClassFolder : IkvmMonikers.JarFile;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }

}
