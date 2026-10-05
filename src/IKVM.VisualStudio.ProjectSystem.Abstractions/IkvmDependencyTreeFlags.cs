using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.ProjectSystem;

/// <summary>
/// Flags of the nodes in the IKVM Dependencies tree that the IKVM extension understands.
/// </summary>
public static class IkvmDependencyTreeFlags
{

    /// <summary>
    /// The IKVM Dependencies node.
    /// </summary>
    public static readonly ProjectTreeFlags Root = ProjectTreeFlags.Create("IkvmDependenciesRoot");

    /// <summary>
    /// A folder grouping the dependencies of one target framework, in projects with more than one. Its caption is the
    /// target framework.
    /// </summary>
    public static readonly ProjectTreeFlags TargetFramework = ProjectTreeFlags.Create("IkvmDependenciesTargetFramework");

    /// <summary>
    /// A node standing for an item of the project. Such a node gets the reference context menu, and can be removed,
    /// which removes the item from the project file. The item is the one named by the context of the node's browse
    /// object, which must therefore be set.
    /// </summary>
    public static readonly ProjectTreeFlags Reference = ProjectTreeFlags.Create("IkvmDependencyReference");

}
