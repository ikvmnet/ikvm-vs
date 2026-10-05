using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

using IKVM.VisualStudio.ProjectSystem.UI;

using Microsoft.Win32;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// The settings of a JAR or class directory in the Manage IKVM Dependencies dialog.
/// </summary>
internal partial class JarDependencyView : UserControl
{

    public JarDependencyView()
    {
        InitializeComponent();

        // the ordered lists: reordered by dragging, and taking drops at a position
        new ListReorder(ClassList, CanEdit, GetDroppedFiles, (dropped, index) =>
        {
            if (dropped is PathItem item)
                Entry?.MoveClass(item.Path, index);
            else if (dropped is string[] paths)
                Entry?.AddClasses(paths, index);
        });
        new ListReorder(SourceList, CanEdit, GetDroppedFiles, (dropped, index) =>
        {
            if (dropped is PathItem item)
                Entry?.MoveSource(item.Path, index);
            else if (dropped is string[] paths)
                Entry?.AddSources(paths, index);
        });
        new ListReorder(DependencyList, CanEdit, GetDroppedReference, (dropped, index) =>
        {
            if (dropped is DependencyOption option)
                Entry?.MoveReference(option.Target, index);
            else if (dropped is JarDependencyEntry target)
                Entry?.MoveReference(target, index);
        });
    }

    JarDependencyEntry? Entry => DataContext as JarDependencyEntry;

    bool CanEdit() => Entry is { CanEdit: true };

    /// <summary>
    /// Gets files dropped from elsewhere, for Compile and Sources.
    /// </summary>
    static object? GetDroppedFiles(IDataObject data)
    {
        return data.GetData(DataFormats.FileDrop) is string[] { Length: > 0 } paths ? paths : null;
    }

    /// <summary>
    /// Gets an entry dragged from the list of the dialog, for "Depends on", if this one can depend on it.
    /// </summary>
    object? GetDroppedReference(IDataObject data)
    {
        return data.GetData(typeof(IkvmDependencyEntry)) is JarDependencyEntry target && Entry is { } entry && entry.Dependencies.Any(i => i.Target == target) ? target : null;
    }

    void OnAddDependency(object sender, RoutedEventArgs e)
    {
        if (Entry is not { } entry || sender is not FrameworkElement button)
            return;

        var menu = new ContextMenu() { PlacementTarget = button, Placement = PlacementMode.Bottom };
        foreach (var option in entry.AddableDependencies)
        {
            var item = new MenuItem() { Header = option, HeaderTemplate = (DataTemplate)FindResource("DependencyMenuHeader") };
            item.Click += (s, a) => option.IsChecked = true;
            menu.Items.Add(item);
        }

        if (menu.Items.Count == 0)
            menu.Items.Add(new MenuItem() { Header = "No other references", IsEnabled = false });

        menu.IsOpen = true;
    }

    void OnRemoveClass(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PathItem item })
            Entry?.RemoveClass(item.Path);
    }

    void OnRemoveSource(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: PathItem item })
            Entry?.RemoveSource(item.Path);
    }

    void OnRemoveDependency(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: DependencyOption option })
            option.IsChecked = false;
    }

    void OnAddClasses(object sender, RoutedEventArgs e)
    {
        if (Entry is { } entry && PickFiles("Add to Compile", "Java archives (*.jar)|*.jar|All files (*.*)|*.*") is { } paths)
            entry.AddClasses(paths);
    }

    void OnAddSources(object sender, RoutedEventArgs e)
    {
        if (Entry is { } entry && PickFiles("Add Sources", "Source archives (*.jar;*.zip)|*.jar;*.zip|All files (*.*)|*.*") is { } paths)
            entry.AddSources(paths);
    }

    string[]? PickFiles(string title, string filter)
    {
        var dialog = new OpenFileDialog() { Title = title, Filter = filter, Multiselect = true, InitialDirectory = Entry?.Context.ProjectDirectory };
        return dialog.ShowDialog(Window.GetWindow(this)) == true ? dialog.FileNames : null;
    }

}
