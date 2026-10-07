using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem;

/// <summary>
/// Per-configuration state of an IKVM project, exported so other components can reach the configured project and
/// its properties.
/// </summary>
[Export]
[AppliesTo(IkvmProjectCapabilities.AppliesTo)]
internal class IkvmConfiguredProject
{

    /// <summary>
    /// The CPS configured project this instance belongs to.
    /// </summary>
    [Import]
    internal ConfiguredProject ConfiguredProject { get; private set; } = null!;

    /// <summary>
    /// Strongly typed access to the properties of this configuration.
    /// </summary>
    [Import]
    internal ProjectProperties Properties { get; private set; } = null!;

}
