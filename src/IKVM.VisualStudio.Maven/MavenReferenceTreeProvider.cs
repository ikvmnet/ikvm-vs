using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Maven.Imaging;
using IKVM.VisualStudio.ProjectSystem;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace IKVM.VisualStudio.Maven;

/// <summary>
/// Adds the <c>MavenReference</c> items of a project using IKVM.Maven.Sdk under IKVM Dependencies, each with the tree
/// of artifacts Maven resolved for it.
/// </summary>
[Export(typeof(IIkvmDependencyTreeProvider))]
[AppliesTo(MavenReferenceRules.Capability)]
internal sealed class MavenReferenceTreeProvider : IIkvmDependencyTreeProvider
{

    public static readonly ProjectTreeFlags MavenReferenceFlag = ProjectTreeFlags.Create("IkvmMavenReference");
    public static readonly ProjectTreeFlags MavenDependencyFlag = ProjectTreeFlags.Create("IkvmMavenDependency");
    public static readonly ProjectTreeFlags MavenJarFlag = ProjectTreeFlags.Create("IkvmMavenJar");
    public static readonly ProjectTreeFlags MavenOmittedFlag = ProjectTreeFlags.Create("IkvmMavenOmitted");
    public static readonly ProjectTreeFlags MavenOmittedReasonFlag = ProjectTreeFlags.Create("IkvmMavenOmittedReason");

    static readonly ProjectImageMoniker MavenIcon = MavenMonikers.MavenReference.ToProjectSystemType();
    static readonly ProjectImageMoniker MavenWarningIcon = MavenMonikers.MavenReferenceWarning.ToProjectSystemType();
    static readonly ProjectImageMoniker MavenDependencyIcon = MavenMonikers.MavenDependency.ToProjectSystemType();
    static readonly ProjectImageMoniker MavenOmittedIcon = MavenMonikers.MavenOmitted.ToProjectSystemType();
    static readonly ProjectImageMoniker InformationIcon = Microsoft.VisualStudio.Imaging.KnownMonikers.StatusInformation.ToProjectSystemType();

    public IReadOnlyCollection<string> RuleNames { get; } = new[] { MavenReferenceRules.MavenReference, MavenReferenceRules.ResolvedMavenReference };

    public async Task<IProjectTree> UpdateTreeAsync(IIkvmDependencyTreeContext context, IProjectTree parent, CancellationToken cancellationToken)
    {
        var references = MavenReference.Create(context.ConfiguredProject, context.Rules);
        var captions = references.Select(i => i.Coordinates).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        parent = parent.RemoveChildren(i => i.Flags.Contains(MavenReferenceFlag) && captions.Contains(i.Caption) == false);

        foreach (var reference in references)
        {
            cancellationToken.ThrowIfCancellationRequested();

            // without resolution results yet, a reference is not known to be broken
            var broken = reference.Graph != null && reference.IsResolved == false;
            var icon = broken ? MavenWarningIcon : MavenIcon;
            var flags = MavenReferenceFlag + IkvmDependencyTreeFlags.Reference + (broken ? ProjectTreeFlags.BrokenReference : ProjectTreeFlags.ResolvedReference);
            var catalog = await reference.Project.Services.PropertyPagesCatalog!.GetCatalogAsync(PropertyPageContexts.BrowseObject);
            var rule = catalog!.BindToContext(MavenReferenceRules.MavenReference, reference.Project.UnconfiguredProject.FullPath, MavenReferenceRules.ItemType, reference.ItemSpec);

            // once resolved, the resolved coordinates beside the settings written in the project file
            var browseObject = rule;
            if (rule != null && reference.Artifact != null && catalog.GetSchema(MavenReferenceRules.ResolvedMavenReference) is { } resolvedSchema)
                browseObject = reference.Project.Services.ExportProvider.GetExportedValue<IRuleFactory>().CreateResolvedReferencePageRule(resolvedSchema, rule.Context, reference.ItemSpec, reference.Artifact.Properties);

            var node = parent.FindChild(reference.Coordinates, MavenReferenceFlag);
            node = node != null
                ? node.SetProperties(icon: icon, expandedIcon: icon, flags: flags, browseObjectProperties: browseObject)
                : parent.Add(context.NewTree(reference.Coordinates, icon, flags, browseObject));

            node = UpdateJars(context, node, reference.Artifact);
            var path = reference.Artifact != null ? ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, reference.Artifact.ItemSpec) : ImmutableHashSet<string>.Empty;
            node = UpdateDependencies(context, node, reference, reference.Artifact, path, catalog, rule?.Context, cancellationToken);
            parent = node.Parent!;
        }

        return parent;
    }

    /// <summary>
    /// Updates the children of a Maven node to the files of its artifact, from the local Maven repository.
    /// </summary>
    static IProjectTree UpdateJars(IIkvmDependencyTreeContext context, IProjectTree node, MavenArtifact? artifact)
    {
        var paths = artifact?.Compile.ToList() ?? new List<string>();
        var captions = paths.Select(i => System.IO.Path.GetFileName(i.TrimEnd('\\', '/'))).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        node = node.RemoveChildren(i => i.Flags.Contains(MavenJarFlag) && captions.Contains(i.Caption) == false);

        foreach (var path in paths)
        {
            var jar = context.NewJarFileTree(path, MavenJarFlag);
            var existing = node.FindChild(jar.Caption, MavenJarFlag);
            node = existing != null
                ? existing.SetProperties(icon: jar.Icon, expandedIcon: jar.ExpandedIcon, flags: jar.Flags, browseObjectProperties: jar.BrowseObjectProperties).Parent!
                : node.Add(jar).Parent!;
        }

        return node;
    }

    /// <summary>
    /// Updates the children of a Maven node to the artifacts the given artifact depends on, recursively. An artifact
    /// already on the path from the reference is left out, so that cycles end.
    /// </summary>
    static IProjectTree UpdateDependencies(IIkvmDependencyTreeContext context, IProjectTree node, MavenReference reference, MavenArtifact? artifact, ImmutableHashSet<string> path, IPropertyPagesCatalog catalog, IProjectPropertiesContext? itemContext, CancellationToken cancellationToken)
    {
        var dependencies = artifact != null ? reference.GetDependencies(artifact).Where(i => path.Contains(i.ItemSpec) == false).ToList() : new List<MavenArtifact>();
        var captions = dependencies.Select(i => i.Coordinates).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        node = node.RemoveChildren(i => i.Flags.Contains(MavenDependencyFlag) && captions.Contains(i.Caption) == false);

        var schema = catalog.GetSchema(MavenReferenceRules.ResolvedMavenArtifact);
        foreach (var dependency in dependencies)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IRule? rule = null;
            if (schema != null && itemContext != null)
                rule = reference.Project.Services.ExportProvider.GetExportedValue<IRuleFactory>().CreateResolvedReferencePageRule(schema, itemContext, dependency.ItemSpec, dependency.Properties);

            var flags = MavenDependencyFlag + ProjectTreeFlags.ResolvedReference;
            var child = node.FindChild(dependency.Coordinates, MavenDependencyFlag);
            child = child != null
                ? child.SetProperties(icon: MavenDependencyIcon, expandedIcon: MavenDependencyIcon, flags: flags, browseObjectProperties: rule)
                : node.Add(context.NewTree(dependency.Coordinates, MavenDependencyIcon, flags, rule));

            child = UpdateJars(context, child, dependency);
            child = UpdateDependencies(context, child, reference, dependency, path.Add(dependency.ItemSpec), catalog, itemContext, cancellationToken);
            node = child.Parent!;
        }

        return UpdateOmitted(context, node, reference, artifact);
    }

    /// <summary>
    /// Updates the children of a Maven node to the dependencies of its artifact left out because another version of
    /// them won a conflict, each with the reason as its only child.
    /// </summary>
    static IProjectTree UpdateOmitted(IIkvmDependencyTreeContext context, IProjectTree node, MavenReference reference, MavenArtifact? artifact)
    {
        var omitted = artifact?.Omitted.ToList() ?? new List<string>();
        var captions = omitted.ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        node = node.RemoveChildren(i => i.Flags.Contains(MavenOmittedFlag) && captions.Contains(i.Caption) == false);

        foreach (var coordinates in omitted)
        {
            var reason = GetOmittedReason(reference, coordinates);
            var child = node.FindChild(coordinates, MavenOmittedFlag) ?? context.NewTree(coordinates, MavenOmittedIcon, MavenOmittedFlag);
            child = child.RemoveChildren(i => i.Flags.Contains(MavenOmittedReasonFlag) && i.Caption != reason);
            if (child.FindChild(reason, MavenOmittedReasonFlag) == null)
                child = child.Add(context.NewTree(reason, InformationIcon, MavenOmittedReasonFlag)).Parent!;

            node = child.Parent == null ? node.Add(child).Parent! : child.Parent;
        }

        return node;
    }

    /// <summary>
    /// Says why an artifact was left out: which version of it was chosen instead.
    /// </summary>
    static string GetOmittedReason(MavenReference reference, string coordinates)
    {
        var parts = coordinates.Split(':');
        var winner = parts.Length >= 3
            ? reference.Graph?.Values.FirstOrDefault(i => i.GroupId == parts[0] && i.ArtifactId == parts[1] && (parts.Length == 3 || i.Classifier == parts[2]))
            : null;

        return winner != null ? $"Not used: version {winner.Version} was chosen instead" : "Not used: another version was chosen instead";
    }

}
