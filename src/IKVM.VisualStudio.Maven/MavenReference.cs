using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace IKVM.VisualStudio.Maven;

/// <summary>
/// A <c>MavenReference</c> item of a configured project, with the artifacts Maven resolved for the project when
/// design-time resolution has completed.
/// </summary>
internal sealed class MavenReference
{

    /// <summary>
    /// Creates the Maven references described by the rule snapshots of a configured project.
    /// </summary>
    public static ImmutableArray<MavenReference> Create(ConfiguredProject project, IImmutableDictionary<string, IProjectRuleSnapshot> rules)
    {
        if (rules.TryGetValue(MavenReferenceRules.MavenReference, out var evaluated) == false)
            return ImmutableArray<MavenReference>.Empty;

        // the resolved graph is shared by every reference of the project
        ImmutableDictionary<string, MavenArtifact>? graph = null;
        if (rules.TryGetValue(MavenReferenceRules.ResolvedMavenReference, out var resolved) && resolved.Items.Count > 0)
            graph = resolved.Items.ToImmutableDictionary(i => i.Key, i => new MavenArtifact(i.Key, i.Value), StringComparer.OrdinalIgnoreCase);

        return evaluated.Items.Select(i => new MavenReference(project, i.Key, i.Value, graph)).ToImmutableArray();
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public MavenReference(ConfiguredProject project, string itemSpec, IImmutableDictionary<string, string> properties, ImmutableDictionary<string, MavenArtifact>? graph)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        ItemSpec = itemSpec ?? throw new ArgumentNullException(nameof(itemSpec));
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        Graph = graph;

        // coordinates come from metadata, else from an include of the form groupId:artifactId[:version]
        MavenCoordinates.TryParseInclude(itemSpec, out var groupId, out var artifactId, out var version);
        GroupId = Get(MavenReferenceRules.GroupIdMetadata) is { Length: > 0 } g ? g : groupId;
        ArtifactId = Get(MavenReferenceRules.ArtifactIdMetadata) is { Length: > 0 } a ? a : artifactId;
        Version = Get(MavenReferenceRules.VersionMetadata) is { Length: > 0 } v ? v : version ?? "";
        Classifier = Get(MavenReferenceRules.ClassifierMetadata);

        Artifact = graph?.Values.FirstOrDefault(i =>
            string.Equals(i.GroupId, GroupId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(i.ArtifactId, ArtifactId, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(i.Classifier, Classifier, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Configured project the item belongs to.
    /// </summary>
    public ConfiguredProject Project { get; }

    /// <summary>
    /// Evaluated include of the item.
    /// </summary>
    public string ItemSpec { get; }

    /// <summary>
    /// Evaluated metadata of the item.
    /// </summary>
    public IImmutableDictionary<string, string> Properties { get; }

    /// <summary>
    /// The resolved artifacts of the project, by item spec, if design-time resolution has completed.
    /// </summary>
    public ImmutableDictionary<string, MavenArtifact>? Graph { get; }

    public string GroupId { get; }

    public string ArtifactId { get; }

    /// <summary>
    /// The version asked for, which may be a range.
    /// </summary>
    public string Version { get; }

    public string Classifier { get; }

    /// <summary>
    /// The artifact Maven resolved for the reference, if it did.
    /// </summary>
    public MavenArtifact? Artifact { get; }

    public bool IsResolved => Artifact != null;

    /// <summary>
    /// The reference's coordinates, with the version Maven resolved when it did.
    /// </summary>
    public string Coordinates => MavenCoordinates.Format(GroupId, ArtifactId, Classifier, Artifact?.Version ?? Version);

    /// <summary>
    /// Gets the artifacts the given artifact depends on.
    /// </summary>
    public IEnumerable<MavenArtifact> GetDependencies(MavenArtifact artifact)
    {
        if (Graph == null)
            yield break;

        foreach (var itemSpec in artifact.References)
            if (Graph.TryGetValue(itemSpec, out var dependency))
                yield return dependency;
    }

    string Get(string name) => Properties.TryGetValue(name, out var value) ? value ?? "" : "";

}
