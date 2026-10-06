namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// State of an entry in the Manage IKVM Dependencies dialog.
/// </summary>
enum IkvmDependencyState
{

    /// <summary>
    /// Already in the project.
    /// </summary>
    Existing,

    /// <summary>
    /// Added in the dialog; written to the project on save.
    /// </summary>
    New,

    /// <summary>
    /// In the project, and removed from it on save.
    /// </summary>
    Removed,

}
