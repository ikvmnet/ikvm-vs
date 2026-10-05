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

    public string Text { get; }

    public Action Execute { get; }

    public bool IsEnabled { get; }

    public ImageMoniker Icon { get; }

}
