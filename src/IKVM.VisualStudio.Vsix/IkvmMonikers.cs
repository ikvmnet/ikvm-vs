using System;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.Vsix
{

    internal static class IkvmMonikers
    {

        private static readonly Guid ManifestGuid = new Guid("d53d7256-d44d-4245-bdd2-bfd22943659c");

        public static ImageMoniker ProjectIcon => new ImageMoniker { Guid = ManifestGuid, Id = 1 };

    }

}
