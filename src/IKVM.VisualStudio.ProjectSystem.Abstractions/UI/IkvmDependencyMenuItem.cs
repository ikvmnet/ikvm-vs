using System;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// An item of the context menu of an entry in the list of the Manage IKVM Dependencies dialog.
/// </summary>
public sealed class IkvmDependencyMenuItem
{

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="text">Text of the item, with an underscore before its access key.</param>
    /// <param name="execute">What the item does.</param>
    /// <param name="isEnabled">Whether the item can be chosen.</param>
    /// <param name="icon">Icon of the item, if any.</param>
    public IkvmDependencyMenuItem(string text, Action execute, bool isEnabled = true, ImageMoniker icon = default)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Execute = execute ?? throw new ArgumentNullException(nameof(execute));
        IsEnabled = isEnabled;
        Icon = icon;
    }

    /// <summary>
    /// Gets the text of the item, with an underscore before its access key.
    /// </summary>
    public string Text { get; }

    /// <summary>
    /// Gets what the item does when chosen.
    /// </summary>
    public Action Execute { get; }

    /// <summary>
    /// Gets whether the item can be chosen.
    /// </summary>
    public bool IsEnabled { get; }

    /// <summary>
    /// Gets the icon of the item, or the default moniker for none.
    /// </summary>
    public ImageMoniker Icon { get; }

}
