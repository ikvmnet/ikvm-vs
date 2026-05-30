using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem
{

    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(IkvmProjectCapabilities.AppliesTo)]
    [Order(1000)]
    internal class IkvmProjectIconProvider : IProjectTreePropertiesProvider
    {

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (propertyValues.Flags.Contains(ProjectTreeFlags.ProjectRoot))
            {
                var projectMoniker = IkvmMonikers.ProjectIcon.ToProjectSystemType();
                propertyValues.Icon = projectMoniker;
                propertyValues.ExpandedIcon = projectMoniker;
            }
        }

    }

}
