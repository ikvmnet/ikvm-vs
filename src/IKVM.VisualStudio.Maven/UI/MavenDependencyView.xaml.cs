using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// The settings of a Maven reference in the Manage IKVM Dependencies dialog.
/// </summary>
internal partial class MavenDependencyView : UserControl
{

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public MavenDependencyView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The entry the view edits, its data context.
    /// </summary>
    MavenDependencyEntry? Entry => DataContext as MavenDependencyEntry;

    /// <summary>
    /// Adds the exclusion typed in the box to the entry, and clears the box.
    /// </summary>
    void OnAddExclusion(object sender, RoutedEventArgs e)
    {
        Entry?.AddExclusion(ExclusionBox.Text);
        ExclusionBox.Clear();
    }

    /// <summary>
    /// Enter in the exclusion box adds the exclusion typed.
    /// </summary>
    void OnExclusionKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            OnAddExclusion(sender, e);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Removes the exclusion of the clicked row from the entry.
    /// </summary>
    void OnRemoveExclusion(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: MavenExclusionItem item })
            Entry?.RemoveExclusion(item.Exclusion);
    }

    /// <summary>
    /// Excludes the resolved artifact of the clicked row, by adding an exclusion of its group and artifact.
    /// </summary>
    void OnExclude(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: MavenResolvedItem item })
            Entry?.AddExclusion(item.Exclusion);
    }

}
