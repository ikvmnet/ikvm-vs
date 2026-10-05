using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using IKVM.VisualStudio.Vsix.Commands;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.VS;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Supplies the context menus for nodes of the Java References tree.
/// </summary>
[Export(typeof(IProjectItemContextMenuProvider))]
[AppliesTo(IkvmReferenceCapabilities.IkvmReferences)]
[Order(1000)]
internal sealed class JavaReferencesContextMenuProvider : IProjectItemContextMenuProvider
{

    public bool TryGetContextMenu(IProjectTree projectItem, out Guid menuCommandGuid, out int menuCommandId)
    {
        return TryGetMenu(projectItem.Flags, out menuCommandGuid, out menuCommandId);
    }

    public bool TryGetMixedItemsContextMenu(IEnumerable<IProjectTree> projectItems, out Guid menuCommandGuid, out int menuCommandId)
    {
        menuCommandGuid = Guid.Empty;
        menuCommandId = 0;

        // only a selection consisting entirely of Java references gets the reference menu
        var items = projectItems.ToList();
        if (items.Count > 0 && items.All(i => i.Flags.Contains(JavaReferencesTreeProvider.ReferenceFlag)))
        {
            menuCommandGuid = JavaReferenceCommandIds.CommandSet;
            menuCommandId = JavaReferenceCommandIds.JavaReferenceMenu;
            return true;
        }

        return false;
    }

    static bool TryGetMenu(ProjectTreeFlags flags, out Guid menuCommandGuid, out int menuCommandId)
    {
        menuCommandGuid = JavaReferenceCommandIds.CommandSet;

        if (flags.Contains(JavaReferencesTreeProvider.RootFlag) || flags.Contains(JavaReferencesTreeProvider.TargetFrameworkFlag))
        {
            menuCommandId = JavaReferenceCommandIds.JavaReferencesRootMenu;
            return true;
        }

        if (flags.Contains(JavaReferencesTreeProvider.ReferenceFlag))
        {
            menuCommandId = JavaReferenceCommandIds.JavaReferenceMenu;
            return true;
        }

        menuCommandGuid = Guid.Empty;
        menuCommandId = 0;
        return false;
    }

}
