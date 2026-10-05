using System.Collections.Immutable;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace IKVM.VisualStudio.ProjectSystem;

/// <summary>
/// What a <see cref="IIkvmDependencyTreeProvider"/> gets to update its nodes for one configured project.
/// </summary>
public interface IIkvmDependencyTreeContext
{

    /// <summary>
    /// Gets the configured project the nodes are for.
    /// </summary>
    ConfiguredProject ConfiguredProject { get; }

    /// <summary>
    /// Gets the target framework of the configured project, or an empty string for a project with a single one.
    /// </summary>
    string TargetFramework { get; }

    /// <summary>
    /// Gets the latest snapshots of the rules the provider reads, by name. A rule the project does not define is
    /// missing.
    /// </summary>
    IImmutableDictionary<string, IProjectRuleSnapshot> Rules { get; }

    /// <summary>
    /// Creates a node, to be added under the parent or under another node of the provider.
    /// </summary>
    /// <param name="caption">Caption of the node, unique among its siblings with the same flags.</param>
    /// <param name="icon">Icon of the node.</param>
    /// <param name="flags">Flags of the node: at least one of the provider's own, so that it recognizes the node later.</param>
    /// <param name="browseObject">Rule shown in the Properties window for the node.</param>
    /// <param name="expandedIcon">Icon of the node when expanded, if not the same.</param>
    IProjectTree NewTree(string caption, ProjectImageMoniker icon, ProjectTreeFlags flags, IRule? browseObject = null, ProjectImageMoniker? expandedIcon = null);

    /// <summary>
    /// Creates a node for a JAR file or class directory on disk, captioned with its name: with the icon for it, the
    /// JAR file context menu and its location in the Properties window.
    /// </summary>
    /// <param name="fullPath">Full path of the file or directory.</param>
    /// <param name="flags">Flags of the node besides <see cref="IkvmDependencyTreeFlags.JarFile"/>: at least one of the provider's own.</param>
    IProjectTree NewJarFileTree(string fullPath, ProjectTreeFlags flags);

}
