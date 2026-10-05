using System.Linq;
using System.Windows;
using System.Windows.Controls;

using Microsoft.VisualStudio.PlatformUI;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// Asks for the coordinates of a Maven reference to add.
/// </summary>
internal partial class AddMavenReferenceDialog : DialogWindow
{

    public AddMavenReferenceDialog()
    {
        InitializeComponent();
        Loaded += (s, e) => CoordinatesBox.Focus();
    }

    /// <summary>
    /// Gets the coordinates typed, as <c>groupId:artifactId:version</c>.
    /// </summary>
    public string Coordinates => CoordinatesBox.Text.Trim();

    void OnTextChanged(object sender, TextChangedEventArgs e)
    {
        AddButton.IsEnabled = Coordinates.Split(':') is { Length: 3 } parts && parts.All(i => i.Trim().Length > 0);
    }

    void OnAdd(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

}
