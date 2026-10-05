using System.Windows;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// An artifact Maven resolved for a reference, in the list of its dependencies.
/// </summary>
sealed class MavenResolvedItem
{

    public MavenResolvedItem(MavenArtifact artifact, int depth, string? partialNote, bool isRoot)
    {
        Artifact = artifact;
        Depth = depth;
        PartialNote = partialNote;
        IsRoot = isRoot;
    }

    public MavenArtifact Artifact { get; }

    public string Coordinates => Artifact.Coordinates;

    /// <summary>
    /// How deep in the graph the artifact is: 1 for direct dependencies.
    /// </summary>
    public int Depth { get; }

    public Thickness Indent => new Thickness((Depth - 1) * 16, 0, 0, 0);

    /// <summary>
    /// The exclusion that leaves the artifact out.
    /// </summary>
    public string Exclusion => $"{Artifact.GroupId}:{Artifact.ArtifactId}";

    /// <summary>
    /// The target frameworks the artifact is resolved for, when that is only some of those being edited.
    /// </summary>
    public string? PartialNote { get; }

    public bool IsPartial => PartialNote != null;

    public string ToolTip => PartialNote != null ? $"{Coordinates}\n{PartialNote}" : Coordinates;

    /// <summary>
    /// Whether the artifact is the reference itself, which cannot be excluded.
    /// </summary>
    public bool IsRoot { get; }

}
