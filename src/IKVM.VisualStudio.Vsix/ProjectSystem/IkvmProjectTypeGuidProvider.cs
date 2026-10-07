using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem;

/// <summary>
/// Reports the IKVM project type GUID for IKVM projects, so the solution records them as IKVM projects.
/// </summary>
[Export(typeof(IItemTypeGuidProvider))]
[AppliesTo(IkvmProjectCapabilities.AppliesTo)]
internal class IkvmProjectTypeGuidProvider : IItemTypeGuidProvider
{

    /// <summary>
    /// Creates the provider; MEF calls this when composing the project.
    /// </summary>
    [ImportingConstructor]
    public IkvmProjectTypeGuidProvider()
    {

    }

    /// <inheritdoc />
    public Guid ProjectTypeGuid
    {
        get { return ProjectType.IkvmGuid; }
    }

}
