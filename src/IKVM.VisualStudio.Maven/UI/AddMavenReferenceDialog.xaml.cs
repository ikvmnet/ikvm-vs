using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// Finds a Maven artifact to reference, with its version and scope: searching the repositories of the project, or from typed
/// coordinates, with versions from the repositories of the project.
/// </summary>
internal partial class AddMavenReferenceDialog : DialogWindow
{

    readonly AddMavenReferenceViewModel _model;

    /// <summary>
    /// Shows the dialog for a project, which already references some artifacts.
    /// </summary>
    /// <param name="project">The configured project whose repositories are used.</param>
    /// <param name="existing">The <c>groupId:artifactId</c> of the references already in the project, with their
    /// versions.</param>
    public AddMavenReferenceDialog(ConfiguredProject project, IReadOnlyDictionary<string, string> existing)
    {
        InitializeComponent();
        DataContext = _model = new AddMavenReferenceViewModel(project, existing);
        Loaded += (s, e) => SearchBox.Focus();
        Closed += (s, e) => _model.Dispose();
    }

    /// <summary>
    /// Gets the artifact to add, once the dialog is accepted.
    /// </summary>
    public MavenSearchResult? Selected => _model.Selected;

    /// <summary>
    /// Gets the version to add the artifact at, once the dialog is accepted.
    /// </summary>
    public string Version => _model.Version;

    /// <summary>
    /// Gets the scope to add the artifact with, or empty for the default, once the dialog is accepted.
    /// </summary>
    public string Scope => _model.Scope;

    /// <summary>
    /// Down moves from the search to the results, selecting the first.
    /// </summary>
    void OnSearchKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Down || ResultsList.Items.Count == 0)
            return;

        if (ResultsList.SelectedIndex < 0)
            ResultsList.SelectedIndex = 0;

        ResultsList.UpdateLayout();
        (ResultsList.ItemContainerGenerator.ContainerFromIndex(ResultsList.SelectedIndex) as UIElement)?.Focus();
        e.Handled = true;
    }

    /// <summary>
    /// Clears the search and puts the focus back in the search box.
    /// </summary>
    void OnClearSearch(object sender, RoutedEventArgs e)
    {
        _model.SearchText = "";
        SearchBox.Focus();
    }

    /// <summary>
    /// Accepts the dialog when the selected artifact can be added.
    /// </summary>
    void OnAdd(object sender, RoutedEventArgs e)
    {
        if (_model.CanAdd)
            DialogResult = true;
    }

}
