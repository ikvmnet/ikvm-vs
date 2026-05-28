using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem
{

    [Export(typeof(IProjectTreePropertiesProvider))]
    [AppliesTo(ProjectCapabilities.AppliesTo)]
    [Order(1000)]
    internal class IkvmProjectTreePropertiesProvider : IProjectTreePropertiesProvider
    {

        public void CalculatePropertyValues(IProjectTreeCustomizablePropertyContext propertyContext, IProjectTreeCustomizablePropertyValues propertyValues)
        {
            if (propertyValues.Flags.Contains(ProjectTreeFlags.Common.ProjectRoot))
                propertyValues.Icon = IkvmImageMonikers.ProjectIcon.ToProjectSystemType();
        }

    }

}
