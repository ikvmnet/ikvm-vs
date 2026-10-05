using System.IO;

namespace IKVM.VisualStudio.Vsix.UI;

/// <summary>
/// File system facts about a JAR or class directory being added. Assembly names and versions are not derived here:
/// they come from the project's IKVM package, through <see cref="ProjectSystem.References.IkvmReferenceDescriber"/>.
/// </summary>
internal sealed class JarInfo
{

    /// <summary>
    /// Reads the information for the given path. Never throws.
    /// </summary>
    public static JarInfo Read(string path)
    {
        var isDirectory = Directory.Exists(path);
        if (isDirectory)
            return new JarInfo(true, null);

        var sources = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path) + "-sources.jar");
        return new JarInfo(false, File.Exists(sources) ? sources : null);
    }

    /// <summary>
    /// Initializes the information read by <see cref="Read"/>.
    /// </summary>
    JarInfo(bool isDirectory, string? sourcesPath)
    {
        IsDirectory = isDirectory;
        SourcesPath = sourcesPath;
    }

    /// <summary>
    /// Whether the path is an existing directory of classes rather than a JAR.
    /// </summary>
    public bool IsDirectory { get; }

    /// <summary>
    /// A sources JAR found next to the JAR, if any.
    /// </summary>
    public string? SourcesPath { get; }

}
