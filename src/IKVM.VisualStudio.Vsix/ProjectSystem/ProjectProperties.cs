using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace IKVM.VisualStudio.Vsix.ProjectSystem;

/// <summary>
/// Strongly typed access to the properties of an IKVM project configuration, as described by its XAML rules.
/// </summary>
[Export]
[AppliesTo(IkvmProjectCapabilities.AppliesTo)]
internal partial class ProjectProperties : StronglyTypedPropertyAccess
{

    /// <summary>
    /// Creates property access for the project file of the given configuration.
    /// </summary>
    [ImportingConstructor]
    public ProjectProperties(ConfiguredProject configuredProject) :
        base(configuredProject)
    {

    }

    /// <summary>
    /// Creates property access for an item in the given file of the configuration.
    /// </summary>
    public ProjectProperties(ConfiguredProject configuredProject, string file, string itemType, string itemName) :
        base(configuredProject, file, itemType, itemName)
    {

    }

    /// <summary>
    /// Creates property access for the given properties context of the configuration.
    /// </summary>
    public ProjectProperties(ConfiguredProject configuredProject, IProjectPropertiesContext projectPropertiesContext) :
        base(configuredProject, projectPropertiesContext)
    {

    }

    /// <summary>
    /// Creates property access for the given configuration and unconfigured project.
    /// </summary>
    public ProjectProperties(ConfiguredProject configuredProject, UnconfiguredProject unconfiguredProject) :
        base(configuredProject, unconfiguredProject)
    {

    }

}
