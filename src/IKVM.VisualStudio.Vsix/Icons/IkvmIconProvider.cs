using System.ComponentModel.Composition;

using IKVM.VisualStudio.Vsix.ProjectSystem;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.Icons
{

    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(IkvmProjectCapabilities.AppliesTo)]
    [Order(1000)]
    internal class IkvmIconProvider : IProjectTreePropertiesProvider
    {

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            // Project root node: identified by the ProjectRoot flag already set in the property values
            if (propertyValues.Flags.Contains(ProjectTreeFlags.ProjectRoot))
            {
                var projectMoniker = IkvmImageMonikers.ProjectIcon.ToProjectSystemType();
                propertyValues.Icon = projectMoniker;
                propertyValues.ExpandedIcon = projectMoniker;
                return;
            }
        }

    }

}
