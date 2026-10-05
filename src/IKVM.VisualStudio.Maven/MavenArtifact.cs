using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace IKVM.VisualStudio.Maven;

/// <summary>
/// An artifact of the resolved Maven graph of a configured project.
/// </summary>
internal sealed class MavenArtifact
{

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public MavenArtifact(string itemSpec, IImmutableDictionary<string, string> properties)
    {
        ItemSpec = itemSpec ?? throw new ArgumentNullException(nameof(itemSpec));
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
    }

    /// <summary>
    /// Item spec of the resolved item, such as <c>maven$org.slf4j:slf4j-api:2.0.13</c>.
    /// </summary>
    public string ItemSpec { get; }

    /// <summary>
    /// Metadata of the resolved item.
    /// </summary>
    public IImmutableDictionary<string, string> Properties { get; }

    public string GroupId => Get(MavenReferenceRules.ResolvedGroupIdMetadata);

    public string ArtifactId => Get(MavenReferenceRules.ResolvedArtifactIdMetadata);

    public string Classifier => Get(MavenReferenceRules.ResolvedClassifierMetadata);

    public string Version => Get(MavenReferenceRules.ResolvedVersionMetadata);

    /// <summary>
    /// Item specs of the artifacts this one depends on.
    /// </summary>
    public IEnumerable<string> References => Get(MavenReferenceRules.ResolvedReferencesMetadata).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).Where(i => i.Length > 0);

    /// <summary>
    /// Full paths of the files of the artifact compiled into its assembly, in the local Maven repository.
    /// </summary>
    public IEnumerable<string> Compile => Get(MavenReferenceRules.ResolvedCompileMetadata).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).Where(i => i.Length > 0);

    /// <summary>
    /// Direct dependencies of the artifact left out because another version of them won a conflict, as
    /// <c>groupId:artifactId[:classifier]:version</c>.
    /// </summary>
    public IEnumerable<string> Omitted => Get(MavenReferenceRules.ResolvedOmittedMetadata).Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).Where(i => i.Length > 0);

    /// <summary>
    /// The artifact's coordinates, as Maven writes them: <c>groupId:artifactId[:classifier]:version</c>.
    /// </summary>
    public string Coordinates => MavenCoordinates.Format(GroupId, ArtifactId, Classifier, Version);

    string Get(string name) => Properties.TryGetValue(name, out var value) ? value ?? "" : "";

}
