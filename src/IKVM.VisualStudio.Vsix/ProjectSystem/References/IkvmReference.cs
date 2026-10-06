using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// An <c>IkvmReference</c> item of a configured project, with its design-time resolution results when available.
/// </summary>
internal sealed class IkvmReference
{

    /// <summary>
    /// Creates the references described by a rule subscription update of a configured project.
    /// </summary>
    public static ImmutableArray<IkvmReference> Create(ConfiguredProject project, IProjectSubscriptionUpdate update)
    {
        if (update.CurrentState.TryGetValue(IkvmReferenceRules.IkvmReference, out var evaluated) == false)
            return ImmutableArray<IkvmReference>.Empty;

        update.CurrentState.TryGetValue(IkvmReferenceRules.ResolvedIkvmReference, out var resolved);

        var builder = ImmutableArray.CreateBuilder<IkvmReference>();
        foreach (var item in evaluated.Items)
        {
            IImmutableDictionary<string, string>? resolvedProperties = null;
            if (resolved != null)
                resolvedProperties = resolved.Items.FirstOrDefault(i => string.Equals(GetOriginalItemSpec(i.Key, i.Value), item.Key, StringComparison.OrdinalIgnoreCase)).Value;

            builder.Add(new IkvmReference(project, item.Key, item.Value, resolvedProperties));
        }

        return builder.ToImmutable();
    }

    static string GetOriginalItemSpec(string itemSpec, IImmutableDictionary<string, string> properties)
    {
        return properties.TryGetValue(IkvmReferenceRules.OriginalItemSpecMetadata, out var original) && string.IsNullOrEmpty(original) == false ? original : itemSpec;
    }

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public IkvmReference(ConfiguredProject project, string itemSpec, IImmutableDictionary<string, string> properties, IImmutableDictionary<string, string>? resolvedProperties)
    {
        Project = project ?? throw new ArgumentNullException(nameof(project));
        ItemSpec = itemSpec ?? throw new ArgumentNullException(nameof(itemSpec));
        Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        ResolvedProperties = resolvedProperties;
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
    /// Metadata from the design-time resolution of the item, if it has completed.
    /// </summary>
    public IImmutableDictionary<string, string>? ResolvedProperties { get; }

    /// <summary>
    /// Whether the item resolves, as reported by design-time resolution. Without those results, for example when the
    /// design-time build failed, an item that names a JAR or directory without other Compile paths resolves only if
    /// that path exists.
    /// </summary>
    public bool IsResolved
    {
        get
        {
            if (ResolvedProperties != null)
                return ResolvedProperties.TryGetValue(IkvmReferenceRules.IsResolvedMetadata, out var v) == false || string.Equals(v, "true", StringComparison.OrdinalIgnoreCase);

            return IsMissingOnDisk == false;
        }
    }

    /// <summary>
    /// Why the item does not resolve, if it does not.
    /// </summary>
    public string? Diagnostic
    {
        get
        {
            if (ResolvedProperties != null)
                return ResolvedProperties.TryGetValue(IkvmReferenceRules.DiagnosticMetadata, out var v) && string.IsNullOrEmpty(v) == false ? v : null;

            return IsMissingOnDisk ? $"'{ItemSpec}' was not found." : null;
        }
    }

    /// <summary>
    /// Whether the item names a JAR or directory that does not exist, and has no other Compile paths.
    /// </summary>
    bool IsMissingOnDisk
    {
        get
        {
            if (Properties.TryGetValue("Compile", out var compile) && string.IsNullOrWhiteSpace(compile) == false)
                return false;

            var path = Path.Combine(Path.GetDirectoryName(Project.UnconfiguredProject.FullPath) ?? "", ItemSpec);
            return IsDirectory ? Directory.Exists(path) == false : File.Exists(path) == false;
        }
    }

    /// <summary>
    /// Whether the item refers to a class directory rather than a JAR.
    /// </summary>
    public bool IsDirectory => ItemSpec.EndsWith("\\", StringComparison.Ordinal) || ItemSpec.EndsWith("/", StringComparison.Ordinal) || Path.GetExtension(ItemSpec).Length == 0;

    /// <summary>
    /// Name shown for the item.
    /// </summary>
    public string DisplayName
    {
        get
        {
            var path = ItemSpec.TrimEnd('\\', '/');
            var name = Path.GetFileName(path);
            return string.IsNullOrEmpty(name) ? ItemSpec : name;
        }
    }

}
