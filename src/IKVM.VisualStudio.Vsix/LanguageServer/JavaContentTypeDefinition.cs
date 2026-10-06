using System.ComponentModel.Composition;

using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Utilities;

namespace IKVM.VisualStudio.LanguageServer;

/// <summary>
/// The content type of Java source files, which the Java language server serves.
/// </summary>
static class JavaContentTypeDefinition
{

    /// <summary>
    /// The name of the content type.
    /// </summary>
    public const string Name = "java";

    /// <summary>
    /// The content type, served by a language server.
    /// </summary>
    [Export]
    [Name(Name)]
    [BaseDefinition(CodeRemoteContentDefinition.CodeRemoteContentTypeName)]
    internal static ContentTypeDefinition? ContentType { get; set; }

    /// <summary>
    /// Associates <c>.java</c> files with the content type.
    /// </summary>
    [Export]
    [FileExtension(".java")]
    [ContentType(Name)]
    internal static FileExtensionToContentTypeDefinition? FileExtension { get; set; }

}
