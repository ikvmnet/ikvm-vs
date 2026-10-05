using System;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// The styles of the Manage IKVM Dependencies dialog, for entry views to merge into their resources:
/// <c>&lt;ResourceDictionary Source="pack://application:,,,/IKVM.VisualStudio.ProjectSystem.Abstractions;component/UI/IkvmDependencyStyles.xaml" /&gt;</c>.
/// </summary>
/// <remarks>
/// Keys: <c>VariesBrush</c>, <c>ErrorBrush</c>, <c>BoolToVisibility</c>, <c>NotVisible</c>, <c>SectionRule</c>,
/// <c>SectionHeaderPanel</c>, <c>SectionIcon</c>, <c>SectionHeader</c>, <c>RowButton</c>, <c>Placeholder</c>,
/// <c>DropLine</c> and <c>BadgeList</c>, and the style of <see cref="Field"/>.
/// </remarks>
public static class IkvmDependencyStyles
{

    /// <summary>
    /// Gets the location of the styles.
    /// </summary>
    public static Uri Uri { get; } = new Uri("pack://application:,,,/IKVM.VisualStudio.ProjectSystem.Abstractions;component/UI/IkvmDependencyStyles.xaml", UriKind.Absolute);

}
