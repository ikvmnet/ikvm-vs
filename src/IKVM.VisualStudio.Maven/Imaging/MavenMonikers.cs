using System;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.Maven.Imaging;

/// <summary>
/// Monikers for the images in <c>IKVM.VisualStudio.Maven.imagemanifest</c>.
/// </summary>
internal static class MavenMonikers
{

    static readonly Guid ManifestGuid = new Guid("0c74fd5b-929a-469f-9cde-caebe693d0e3");

    /// <summary>
    /// Gets the moniker of an image of the manifest by its ID.
    /// </summary>
    static ImageMoniker Get(int id) => new ImageMoniker { Guid = ManifestGuid, Id = id };

    /// <summary>
    /// Image of a <c>MavenReference</c> item.
    /// </summary>
    public static ImageMoniker MavenReference => Get(1);

    /// <summary>
    /// Image of a <c>MavenReference</c> item Maven did not resolve.
    /// </summary>
    public static ImageMoniker MavenReferenceWarning => Get(2);

    /// <summary>
    /// Image of an artifact Maven resolved as a dependency of a reference.
    /// </summary>
    public static ImageMoniker MavenDependency => Get(3);

    /// <summary>
    /// Image of a dependency left out because another version of it won a conflict.
    /// </summary>
    public static ImageMoniker MavenOmitted => Get(4);

}
