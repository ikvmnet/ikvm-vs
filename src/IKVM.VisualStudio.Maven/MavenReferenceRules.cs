namespace IKVM.VisualStudio.Maven;

/// <summary>
/// Names of the XAML rules, capability and metadata shipped by IKVM.Maven.Sdk for <c>MavenReference</c> items.
/// </summary>
static class MavenReferenceRules
{

    /// <summary>
    /// Capability of projects that use IKVM.Maven.Sdk.
    /// </summary>
    public const string Capability = "MavenReferences";

    public const string ItemType = "MavenReference";

    /// <summary>
    /// Rule over the evaluated <c>MavenReference</c> items.
    /// </summary>
    public const string MavenReference = "MavenReference";

    /// <summary>
    /// Rule over the results of the <c>ResolveMavenReferencesDesignTime</c> target: one item per artifact in the
    /// resolved graph.
    /// </summary>
    public const string ResolvedMavenReference = "ResolvedMavenReference";

    /// <summary>
    /// Read-only rule over a single artifact of the resolved graph, for the Properties window.
    /// </summary>
    public const string ResolvedMavenArtifact = "ResolvedMavenArtifact";

    public const string GroupIdMetadata = "GroupId";
    public const string ArtifactIdMetadata = "ArtifactId";
    public const string ClassifierMetadata = "Classifier";
    public const string VersionMetadata = "Version";

    public const string ResolvedGroupIdMetadata = "MavenGroupId";
    public const string ResolvedArtifactIdMetadata = "MavenArtifactId";
    public const string ResolvedClassifierMetadata = "MavenClassifier";
    public const string ResolvedVersionMetadata = "MavenVersion";
    public const string ResolvedReferencesMetadata = "References";

}
