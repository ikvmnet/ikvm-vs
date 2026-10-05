using System.Collections.Generic;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// The values a Maven entry of the Manage IKVM Dependencies dialog has for one target framework.
/// </summary>
sealed class MavenDependencyValues
{

    /// <summary>
    /// The version asked for, which may be a range.
    /// </summary>
    public string Version { get; set; } = "";

    /// <summary>
    /// The classifier, or empty for none.
    /// </summary>
    public string Classifier { get; set; } = "";

    /// <summary>
    /// The Maven scope, or empty for the default, compile.
    /// </summary>
    public string Scope { get; set; } = "";

    /// <summary>
    /// Whether the dependency is optional, and so not passed on to projects that depend on this one.
    /// </summary>
    public bool Optional { get; set; }

    /// <summary>
    /// Transitive dependencies left out, each as <c>groupId:artifactId[:classifier[:extension]]</c>.
    /// </summary>
    public List<string> Exclusions { get; } = new List<string>();

    /// <summary>
    /// What Maven resolved for the reference in this target framework, once known.
    /// </summary>
    public MavenReference? Resolved { get; set; }

}
