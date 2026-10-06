using System;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.ProjectSystem;

/// <summary>
/// Helpers for updating nodes of the IKVM Dependencies tree.
/// </summary>
public static class IkvmDependencyTreeExtensions
{

    /// <summary>
    /// Finds the child with the given caption, ignoring case, among the children with the given flag.
    /// </summary>
    public static IProjectTree? FindChild(this IProjectTree parent, string caption, ProjectTreeFlags flag)
    {
        return parent.Children.FirstOrDefault(i => i.Flags.Contains(flag) && string.Equals(i.Caption, caption, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Removes the children matching the predicate, returning the updated parent.
    /// </summary>
    public static IProjectTree RemoveChildren(this IProjectTree parent, Func<IProjectTree, bool> predicate)
    {
        foreach (var child in parent.Children.Where(predicate).ToList())
            parent = parent.Remove(parent.Children.First(i => i.Identity == child.Identity));

        return parent;
    }

}
