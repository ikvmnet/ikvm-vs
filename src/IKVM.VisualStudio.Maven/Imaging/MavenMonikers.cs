using System;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.Maven.Imaging;

/// <summary>
/// Monikers for the images in <c>IKVM.VisualStudio.Maven.imagemanifest</c>.
/// </summary>
internal static class MavenMonikers
{

    static readonly Guid ManifestGuid = new Guid("0c74fd5b-929a-469f-9cde-caebe693d0e3");

    static ImageMoniker Get(int id) => new ImageMoniker { Guid = ManifestGuid, Id = id };

    public static ImageMoniker MavenReference => Get(1);

    public static ImageMoniker MavenReferenceWarning => Get(2);

    public static ImageMoniker MavenDependency => Get(3);

}
