using System.Windows;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// An artifact Maven resolved for a reference, in the list of its dependencies.
/// </summary>
sealed class MavenResolvedItem
{

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public MavenResolvedItem(MavenArtifact artifact, int depth, string? partialNote, bool isRoot)
    {
        Artifact = artifact;
        Depth = depth;
        PartialNote = partialNote;
        IsRoot = isRoot;
    }

    /// <summary>
    /// The resolved artifact.
    /// </summary>
    public MavenArtifact Artifact { get; }

    /// <summary>
    /// The artifact's coordinates, which the list shows for it.
    /// </summary>
    public string Coordinates => Artifact.Coordinates;

    /// <summary>
    /// How deep in the graph the artifact is: 1 for direct dependencies.
    /// </summary>
    public int Depth { get; }

    /// <summary>
    /// The margin the list indents the artifact by, to show its depth in the graph.
    /// </summary>
    public Thickness Indent => new Thickness((Depth - 1) * 16, 0, 0, 0);

    /// <summary>
    /// The exclusion that leaves the artifact out.
    /// </summary>
    public string Exclusion => $"{Artifact.GroupId}:{Artifact.ArtifactId}";

    /// <summary>
    /// The target frameworks the artifact is resolved for, when that is only some of those being edited.
    /// </summary>
    public string? PartialNote { get; }

    /// <summary>
    /// Whether the artifact is resolved for only some of the target frameworks being edited, which the list shows it
    /// differently for.
    /// </summary>
    public bool IsPartial => PartialNote != null;

    /// <summary>
    /// The tooltip of the artifact in the list: its coordinates, and the target frameworks it is resolved for when
    /// that is only some.
    /// </summary>
    public string ToolTip => PartialNote != null ? $"{Coordinates}\n{PartialNote}" : Coordinates;

    /// <summary>
    /// Whether the artifact is the reference itself, which cannot be excluded.
    /// </summary>
    public bool IsRoot { get; }

}
