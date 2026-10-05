namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// The state a <see cref="Field"/> shows with its border.
/// </summary>
public enum FieldState
{

    /// <summary>
    /// The value is the same for every target framework being edited, and valid.
    /// </summary>
    Normal,

    /// <summary>
    /// The value differs between the target frameworks being edited.
    /// </summary>
    Varies,

    /// <summary>
    /// The value is not valid.
    /// </summary>
    Error,

}
