using System;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.Vsix.Imaging;

/// <summary>
/// Monikers for the images in <c>IKVM.VisualStudio.imagemanifest</c>.
/// </summary>
internal static class IkvmMonikers
{

    static readonly Guid ManifestGuid = new Guid("d53d7256-d44d-4245-bdd2-bfd22943659c");

    static ImageMoniker Get(int id) => new ImageMoniker { Guid = ManifestGuid, Id = id };

    public static ImageMoniker ProjectIcon => Get(1);

    public static ImageMoniker IkvmDependencies => Get(2);

    public static ImageMoniker JarFile => Get(3);

    public static ImageMoniker JarFileWarning => Get(4);

    public static ImageMoniker JarFilePending => Get(5);

    public static ImageMoniker ClassFolder => Get(6);

    public static ImageMoniker ClassFolderWarning => Get(7);

    public static ImageMoniker JavaClass => Get(8);

    public static ImageMoniker ClassFile => Get(9);

    public static ImageMoniker JavaSource => Get(10);

    public static ImageMoniker JavaPackage => Get(11);

    public static ImageMoniker MavenReference => Get(12);

    public static ImageMoniker MavenReferenceWarning => Get(13);

    public static ImageMoniker MavenDependency => Get(14);

}
