using System.Collections.Generic;
using System.Linq;

using Microsoft.Build.Construction;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Identifies elements of project files across configured projects by their file and position among its elements.
/// Line and column numbers cannot: elements added since the file was last loaded have none.
/// </summary>
sealed class ElementIdentities
{

    readonly Dictionary<ProjectRootElement, Dictionary<ProjectElement, int>> _positions = new Dictionary<ProjectRootElement, Dictionary<ProjectElement, int>>();

    /// <summary>
    /// Gets an identity for <paramref name="element"/> made of its file's full path and its position among that file's
    /// elements, or -1 when the element is not found. Positions are computed once per file and cached.
    /// </summary>
    public string Get(ProjectElement element)
    {
        var root = element.ContainingProject;
        if (_positions.TryGetValue(root, out var positions) == false)
        {
            positions = root.AllChildren.Select((e, i) => (e, i)).ToDictionary(i => i.e, i => i.i);
            _positions[root] = positions;
        }

        return root.FullPath + "|" + (positions.TryGetValue(element, out var position) ? position : -1);
    }

}
