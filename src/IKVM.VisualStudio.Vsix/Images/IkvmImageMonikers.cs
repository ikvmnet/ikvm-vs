using System;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.Vsix.Images
{

    internal static class IkvmImageMonikers
    {

        private static readonly Guid ManifestGuid = new Guid("99a611b0-1654-4403-b5b6-7b72d999d621");

        public static ImageMoniker ProjectIcon => new ImageMoniker { Guid = ManifestGuid, Id = 0 };

    }

}
