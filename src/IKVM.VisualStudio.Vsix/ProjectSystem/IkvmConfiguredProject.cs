using System.ComponentModel.Composition;

using IKVM.VisualStudio.Vsix.Packaging;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem
{

    [Export]
    [AppliesTo(ProjectTypeCapabilities.AppliesTo)]
    internal class IkvmConfiguredProject
    {

        [Import]
        internal ConfiguredProject ConfiguredProject { get; private set; } = null!;

        [Import]
        internal ProjectProperties Properties { get; private set; } = null!;

    }

}
