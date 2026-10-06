using IKVM.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// An existing element and the state it should have.
/// </summary>
internal sealed record IkvmDependencyElementUpdate(IkvmDependencyElement Original, IkvmDependencyElement Updated);
