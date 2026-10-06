using System.Collections.Generic;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// The values a JAR entry of the Manage IKVM Dependencies dialog has for one target framework.
/// </summary>
sealed class JarDependencyValues
{

    public string AssemblyName { get; set; } = "";

    public string AssemblyVersion { get; set; } = "";

    /// <summary>
    /// Full paths of the classes compiled into the assembly, in order. Compile when it is set, else the entry itself,
    /// its default.
    /// </summary>
    public List<string> Classes { get; } = new List<string>();

    /// <summary>
    /// Full paths of the source archives.
    /// </summary>
    public List<string> Sources { get; } = new List<string>();

    /// <summary>
    /// Other entries this entry references.
    /// </summary>
    public List<JarDependencyEntry> References { get; } = new List<JarDependencyEntry>();

}
