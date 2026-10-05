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

    static readonly ProjectImageMoniker MavenIcon = MavenMonikers.MavenReference.ToProjectSystemType();
    static readonly ProjectImageMoniker MavenWarningIcon = MavenMonikers.MavenReferenceWarning.ToProjectSystemType();
    static readonly ProjectImageMoniker MavenDependencyIcon = MavenMonikers.MavenDependency.ToProjectSystemType();

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

            var path = reference.Artifact != null ? ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, reference.Artifact.ItemSpec) : ImmutableHashSet<string>.Empty;
            node = UpdateDependencies(context, node, reference, reference.Artifact, path, catalog, rule?.Context, cancellationToken);
            parent = node.Parent!;
        }

        return parent;
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

            child = UpdateDependencies(context, child, reference, dependency, path.Add(dependency.ItemSpec), catalog, itemContext, cancellationToken);
            node = child.Parent!;
        }

        return node;
    }

}
