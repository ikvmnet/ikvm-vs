using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using IKVM.VisualStudio.ProjectSystem;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Supplies the context menus for nodes of the IKVM Dependencies tree.
/// </summary>
[Export(typeof(IProjectItemContextMenuProvider))]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
[Order(1000)]
internal sealed class IkvmDependenciesContextMenuProvider : IProjectItemContextMenuProvider
{

    public bool TryGetContextMenu(IProjectTree projectItem, out Guid menuCommandGuid, out int menuCommandId)
    {
        return TryGetMenu(projectItem.Flags, out menuCommandGuid, out menuCommandId);
    }

    public bool TryGetMixedItemsContextMenu(IEnumerable<IProjectTree> projectItems, out Guid menuCommandGuid, out int menuCommandId)
    {
        menuCommandGuid = Guid.Empty;
        menuCommandId = 0;

        // only a selection consisting entirely of IKVM references gets the reference menu
        var items = projectItems.ToList();
        if (items.Count > 0 && items.All(i => i.Flags.Contains(IkvmDependencyTreeFlags.Reference)))
        {
            menuCommandGuid = IkvmDependencyCommandIds.CommandSet;
            menuCommandId = IkvmDependencyCommandIds.IkvmReferenceMenu;
            return true;
        }

        return false;
    }

    static bool TryGetMenu(ProjectTreeFlags flags, out Guid menuCommandGuid, out int menuCommandId)
    {
        menuCommandGuid = IkvmDependencyCommandIds.CommandSet;

        if (flags.Contains(IkvmDependencyTreeFlags.Root) || flags.Contains(IkvmDependencyTreeFlags.TargetFramework))
        {
            menuCommandId = IkvmDependencyCommandIds.IkvmDependenciesRootMenu;
            return true;
        }

        if (flags.Contains(IkvmDependencyTreeFlags.Reference))
        {
            menuCommandId = IkvmDependencyCommandIds.IkvmReferenceMenu;
            return true;
        }

        menuCommandGuid = Guid.Empty;
        menuCommandId = 0;
        return false;
    }

}
