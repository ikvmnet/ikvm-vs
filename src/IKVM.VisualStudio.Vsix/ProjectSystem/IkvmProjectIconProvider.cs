using System.ComponentModel.Composition;

using IKVM.VisualStudio.Vsix.Imaging;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem;

/// <summary>
/// Gives the root node of an IKVM project the IKVM project icon in Solution Explorer.
/// </summary>
[Export(typeof(IProjectTreePropertiesProvider))]
[AppliesTo(IkvmProjectCapabilities.AppliesTo)]
[Order(1000)]
internal class IkvmProjectIconProvider : IProjectTreePropertiesProvider
{

    /// <inheritdoc />
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
