using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// A button above the list of the Manage IKVM Dependencies dialog that adds entries.
/// </summary>
public sealed class IkvmDependencyAddCommand
{

    readonly Func<Window, Task<IReadOnlyList<IkvmDependencyEntry>>> _execute;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="text">Text of the button, such as <c>JARs...</c>.</param>
    /// <param name="icon">Icon of the button.</param>
    /// <param name="description">Accessible name of the button, such as <c>Add JAR files</c>; also, followed by an ellipsis, its text in the context menu of the IKVM Dependencies node.</param>
    /// <param name="execute">Asks for what to add, given the dialog as the owner of any window it opens, and returns the new entries.</param>
    public IkvmDependencyAddCommand(string text, ImageMoniker icon, string description, Func<Window, Task<IReadOnlyList<IkvmDependencyEntry>>> execute)
    {
        Text = text ?? throw new ArgumentNullException(nameof(text));
        Icon = icon;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public string Text { get; }

    public ImageMoniker Icon { get; }

    public string Description { get; }

    internal Task<IReadOnlyList<IkvmDependencyEntry>> ExecuteAsync(Window owner) => _execute(owner);

}
