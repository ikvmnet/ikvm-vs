namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// What the project's IKVM package derives for a JAR or class directory.
/// </summary>
internal sealed record JavaReferenceDescription(string Path, string? AssemblyName, string? AssemblyVersion, bool IsResolved, string? Diagnostic);
