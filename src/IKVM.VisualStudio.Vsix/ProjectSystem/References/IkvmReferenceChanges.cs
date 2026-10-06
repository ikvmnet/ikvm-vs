using System.Collections.Generic;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Changes to the <c>IkvmReference</c> elements of a project, applied together.
/// </summary>
/// <param name="Removed">Existing elements to remove.</param>
/// <param name="Updated">Existing elements to change in place, or move to another target framework.</param>
/// <param name="Added">Elements to add.</param>
internal sealed record IkvmReferenceChanges(IReadOnlyList<IkvmReferenceElement> Removed, IReadOnlyList<IkvmReferenceElementUpdate> Updated, IReadOnlyList<IkvmReferenceElement> Added)
{

    public bool IsEmpty => Removed.Count == 0 && Updated.Count == 0 && Added.Count == 0;

}
