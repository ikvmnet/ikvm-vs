using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// The settings of a Maven reference in the Manage IKVM Dependencies dialog.
/// </summary>
internal partial class MavenDependencyView : UserControl
{

    public MavenDependencyView()
    {
        InitializeComponent();
    }

    MavenDependencyEntry? Entry => DataContext as MavenDependencyEntry;

    void OnAddExclusion(object sender, RoutedEventArgs e)
    {
        Entry?.AddExclusion(ExclusionBox.Text);
        ExclusionBox.Clear();
    }

    void OnExclusionKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnAddExclusion(sender, e);
            e.Handled = true;
        }
    }

    void OnRemoveExclusion(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: MavenExclusionItem item })
            Entry?.RemoveExclusion(item.Exclusion);
    }

    void OnExclude(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: MavenResolvedItem item })
            Entry?.AddExclusion(item.Exclusion);
    }

}
