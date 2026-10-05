using System.Collections.Generic;
using System.Windows;

using Microsoft.VisualStudio.PlatformUI;
using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// Finds a Maven artifact to reference, with its version and scope: searching Maven Central, or from typed
/// coordinates, with versions from the repositories of the project.
/// </summary>
internal partial class AddMavenReferenceDialog : DialogWindow
{

    readonly AddMavenReferenceViewModel _model;

    /// <param name="project">The configured project whose repositories are used.</param>
    /// <param name="existing">The <c>groupId:artifactId</c> of the references already in the project.</param>
    public AddMavenReferenceDialog(ConfiguredProject project, IEnumerable<string> existing)
    {
        InitializeComponent();
        DataContext = _model = new AddMavenReferenceViewModel(project, existing);
        Loaded += (s, e) => SearchBox.Focus();
    }

    /// <summary>
    /// Gets the artifact to add, once the dialog is accepted.
    /// </summary>
    public MavenSearchResult? Selected => _model.Selected;

    public string Version => _model.Version;

    public string Scope => _model.Scope;

    void OnAdd(object sender, RoutedEventArgs e)
    {
        if (_model.CanAdd)
            DialogResult = true;
    }

}
