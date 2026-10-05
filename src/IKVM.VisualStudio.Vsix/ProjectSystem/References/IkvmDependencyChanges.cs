using System.Collections.Generic;

using IKVM.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Changes to the <c>IkvmReference</c> elements of a project, applied together.
/// </summary>
/// <param name="Removed">Existing elements to remove.</param>
/// <param name="Updated">Existing elements to change in place, or move to another target framework.</param>
/// <param name="Added">Elements to add.</param>
internal sealed record IkvmDependencyChanges(IReadOnlyList<IkvmDependencyElement> Removed, IReadOnlyList<IkvmDependencyElementUpdate> Updated, IReadOnlyList<IkvmDependencyElement> Added)
{

    /// <summary>
    /// Whether there is nothing to remove, update or add, so the project file need not be touched.
    /// </summary>
    public bool IsEmpty => Removed.Count == 0 && Updated.Count == 0 && Added.Count == 0;

}
