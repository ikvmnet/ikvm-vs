namespace IKVM.VisualStudio.Host.Maven.Contracts;

/// <summary>
/// How a repository is searched.
/// </summary>
public enum MavenServiceSearchMethod
{

    /// <summary>
    /// The repository cannot be searched: it has no search service, and publishes no index.
    /// </summary>
    None,

    /// <summary>
    /// The search service of the repository: that of Maven Central, Nexus or Artifactory.
    /// </summary>
    Server,

    /// <summary>
    /// The index the repository publishes, downloaded and kept up to date in the background.
    /// </summary>
    Index,

}
