using System;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;

using Microsoft.Build.Framework.XamlTypes;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// What the Properties window shows for a JAR file or class directory on disk: where it is, how large it is and when
/// it changed, all read-only.
/// </summary>
static class JarFileBrowseObject
{

    const string ItemType = "IkvmJarFile";

    static readonly Lazy<Rule> Schema = new Lazy<Rule>(CreateSchema);

    static Rule CreateSchema()
    {
        var rule = new Rule() { Name = ItemType, DisplayName = "JAR File", Description = "JAR File Properties", PageTemplate = "generic" };
        rule.BeginInit();
        rule.DataSource = new DataSource() { Persistence = "ResolvedReference", ItemType = ItemType, HasConfigurationCondition = false, SourceOfDefaultValue = DefaultValueSourceLocation.AfterContext };
        rule.Categories.Add(new Category() { Name = "File", DisplayName = "File" });
        rule.Properties.Add(Property("FileName", "File Name", "Name of the file or directory."));
        rule.Properties.Add(Property("FullPath", "Full Path", "Where the file or directory is on disk."));
        rule.Properties.Add(Property("Folder", "Folder", "The directory holding the file or directory."));
        rule.Properties.Add(Property("Size", "Size", "Size of the file."));
        rule.Properties.Add(Property("Modified", "Modified", "When the file or directory last changed."));
        rule.EndInit();
        return rule;
    }

    static StringProperty Property(string name, string displayName, string description)
    {
        return new StringProperty() { Name = name, DisplayName = displayName, Description = description, Category = "File", ReadOnly = true };
    }

    /// <summary>
    /// Creates the browse object for the given path, in a configured project.
    /// </summary>
    public static IRule Create(ConfiguredProject project, string fullPath)
    {
        var properties = ImmutableDictionary.CreateBuilder<string, string>(StringComparer.OrdinalIgnoreCase);
        properties["FileName"] = Path.GetFileName(fullPath.TrimEnd('\\', '/'));
        properties["FullPath"] = fullPath;
        properties["Folder"] = Path.GetDirectoryName(fullPath.TrimEnd('\\', '/')) ?? "";

        try
        {
            if (File.Exists(fullPath))
            {
                var file = new FileInfo(fullPath);
                properties["Size"] = FormatSize(file.Length);
                properties["Modified"] = file.LastWriteTime.ToString("g", CultureInfo.CurrentCulture);
            }
            else if (Directory.Exists(fullPath))
            {
                properties["Modified"] = Directory.GetLastWriteTime(fullPath).ToString("g", CultureInfo.CurrentCulture);
            }
            else
            {
                properties["Size"] = "Not found";
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            properties["Size"] = e.Message;
        }

        var context = new FileContext(project.UnconfiguredProject.FullPath, fullPath);
        return project.Services.ExportProvider.GetExportedValue<IRuleFactory>().CreateResolvedReferencePageRule(Schema.Value, context, fullPath, properties.ToImmutable());
    }

    static string FormatSize(long bytes)
    {
        if (bytes < 1024)
            return $"{bytes:N0} bytes";
        if (bytes < 1024 * 1024)
            return $"{bytes / 1024.0:N1} KB ({bytes:N0} bytes)";

        return $"{bytes / (1024.0 * 1024.0):N1} MB ({bytes:N0} bytes)";
    }

    /// <summary>
    /// The item the browse object describes: the file, which is not an item of the project.
    /// </summary>
    sealed class FileContext : IProjectPropertiesContext
    {

        public FileContext(string projectPath, string fullPath)
        {
            File = projectPath;
            ItemName = fullPath;
        }

        public bool IsProjectFile => true;

        public string File { get; }

        public string ItemType => JarFileBrowseObject.ItemType;

        public string ItemName { get; }

    }

}
