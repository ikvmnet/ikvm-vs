using System;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.Vsix.Imaging;

/// <summary>
/// Monikers for the images in <c>IKVM.VisualStudio.imagemanifest</c>.
/// </summary>
internal static class IkvmMonikers
{

    static readonly Guid ManifestGuid = new Guid("d53d7256-d44d-4245-bdd2-bfd22943659c");

    /// <summary>
    /// Returns the moniker for the image with the given id in the IKVM image manifest.
    /// </summary>
    static ImageMoniker Get(int id) => new ImageMoniker { Guid = ManifestGuid, Id = id };

    /// <summary>
    /// Icon shown on the root node of an IKVM project.
    /// </summary>
    public static ImageMoniker ProjectIcon => Get(1);

    /// <summary>
    /// Icon for the IKVM node in the Dependencies tree.
    /// </summary>
    public static ImageMoniker IkvmDependencies => Get(2);

    /// <summary>
    /// Icon for a JAR file reference.
    /// </summary>
    public static ImageMoniker JarFile => Get(3);

    /// <summary>
    /// Icon for a JAR file reference that did not resolve, such as one whose file is missing.
    /// </summary>
    public static ImageMoniker JarFileWarning => Get(4);

    /// <summary>
    /// Icon for a class folder reference.
    /// </summary>
    public static ImageMoniker ClassFolder => Get(6);

    /// <summary>
    /// Icon for a class folder reference that did not resolve, such as one whose directory is missing.
    /// </summary>
    public static ImageMoniker ClassFolderWarning => Get(7);

    /// <summary>
    /// Icon for a compiled <c>.class</c> file.
    /// </summary>
    public static ImageMoniker ClassFile => Get(9);

    /// <summary>
    /// Icon for a <c>.java</c> source file.
    /// </summary>
    public static ImageMoniker JavaSource => Get(10);

}
