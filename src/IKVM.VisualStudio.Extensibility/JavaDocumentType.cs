using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.LanguageServer;

namespace IKVM.VisualStudio.Extensibility;

/// <summary>
/// The Java document type: <c>.java</c> files, which the Java language server serves.
/// </summary>
internal static class JavaDocumentType
{

    /// <summary>
    /// Declares the document type of Java source files.
    /// </summary>
    [VisualStudioContribution]
    internal static DocumentTypeConfiguration Configuration => new("java")
    {
        FileExtensions = [".java"],
        BaseDocumentType = LanguageServerProvider.LanguageServerBaseDocumentType,
    };

}
