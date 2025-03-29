using System;
using System.ComponentModel.Composition;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem
{

    [Export(typeof(IItemTypeGuidProvider))]
    [AppliesTo("IKVM")]
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
