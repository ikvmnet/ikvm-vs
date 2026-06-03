using IKVM.VisualStudio.Vsix.Registration;

using Microsoft.VisualStudio.Shell;

using System;
using System.Runtime.InteropServices;

namespace IKVM.VisualStudio.Vsix.Packaging
{

    [Guid(PackageGuid)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0")]
    [ProvideBindingPath]
    [ProvideImageManifest("IKVM.VisualStudio.imagemanifest")]
    public sealed class IkvmPackage : AsyncPackage
    {

        public const string PackageGuid = "c3f18b47-662b-4785-a2bf-5fd7bc8c6f69";

    }

}
