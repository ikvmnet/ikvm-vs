using Microsoft.VisualStudio.Extensibility;

namespace IKVM.VisualStudio.Extensibility;

/// <summary>
/// The VisualStudio.Extensibility extension of IKVM: the parts that Visual Studio activates itself, such as the Java
/// language server provider. They run in the extension host Visual Studio shares between extensions, and hand their
/// work to the IKVM host.
/// </summary>
[VisualStudioContribution]
internal sealed class IkvmExtension : Extension
{

    /// <inheritdoc />
    public override ExtensionConfiguration ExtensionConfiguration => new()
    {
        RequiresInProcessHosting = false,
        Metadata = new(
            id: "IKVM.VisualStudio.8bf8ccd4-363a-4a23-a857-b038314c2c9e",
            version: ExtensionAssemblyVersion,
            publisherName: "IKVM",
            displayName: "IKVM",
            description: "IKVM support for Visual Studio"),
    };

}
