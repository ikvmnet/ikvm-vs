using System;
using System.ComponentModel.Composition;

using IKVM.VisualStudio.Vsix.Packaging;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem
{

    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo(ProjectTypeCapabilities.AppliesTo)]
    internal class IkvmProjectTypeGuidProvider : IItemTypeGuidProvider
    {

        [ImportingConstructor]
        public IkvmProjectTypeGuidProvider()
        {

        }

        public Guid ProjectTypeGuid
        {
            get { return ProjectType.IkvmGuid; }
        }

    }

}
