namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// An existing <c>IkvmReference</c> element and the state it should have.
/// </summary>
internal sealed record IkvmReferenceElementUpdate(IkvmReferenceElement Original, IkvmReferenceElement Updated);
