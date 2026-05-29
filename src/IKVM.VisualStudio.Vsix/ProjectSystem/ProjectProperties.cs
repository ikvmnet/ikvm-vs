using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace IKVM.VisualStudio.Vsix.ProjectSystem
{

    [Export]
    [AppliesTo(IkvmProjectCapabilities.AppliesTo)]
    internal partial class ProjectProperties : StronglyTypedPropertyAccess
    {

        [ImportingConstructor]
        public ProjectProperties(ConfiguredProject configuredProject) :
            base(configuredProject)
        {
        }

        public ProjectProperties(ConfiguredProject configuredProject, string file, string itemType, string itemName) :
            base(configuredProject, file, itemType, itemName)
        {
        }

        public ProjectProperties(ConfiguredProject configuredProject, IProjectPropertiesContext projectPropertiesContext) :
            base(configuredProject, projectPropertiesContext)
        {
        }

        public ProjectProperties(ConfiguredProject configuredProject, UnconfiguredProject unconfiguredProject) :
            base(configuredProject, unconfiguredProject)
        {
        }

    }

}
