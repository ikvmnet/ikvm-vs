using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.Vsix.Imaging;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Adds the <c>IkvmReference</c> items of a project under IKVM Dependencies.
/// </summary>
[Export(typeof(IIkvmDependencyTreeProvider))]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
internal sealed class IkvmReferenceTreeProvider : IIkvmDependencyTreeProvider
{

    public static readonly ProjectTreeFlags ReferenceFlag = ProjectTreeFlags.Create("IkvmReference");

    static readonly ProjectImageMoniker JarIcon = IkvmMonikers.JarFile.ToProjectSystemType();
    static readonly ProjectImageMoniker JarWarningIcon = IkvmMonikers.JarFileWarning.ToProjectSystemType();
    static readonly ProjectImageMoniker FolderIcon = IkvmMonikers.ClassFolder.ToProjectSystemType();
    static readonly ProjectImageMoniker FolderWarningIcon = IkvmMonikers.ClassFolderWarning.ToProjectSystemType();

    public IReadOnlyCollection<string> RuleNames { get; } = new[] { IkvmReferenceRules.IkvmReference, IkvmReferenceRules.ResolvedIkvmReference };

    public async Task<IProjectTree> UpdateTreeAsync(IIkvmDependencyTreeContext context, IProjectTree parent, CancellationToken cancellationToken)
    {
        var references = IkvmReference.Create(context.ConfiguredProject, context.Rules).ToList();
        var captions = GetCaptions(references);
        parent = parent.RemoveChildren(i => i.Flags.Contains(ReferenceFlag) && captions.Values.Contains(i.Caption) == false);

        foreach (var reference in references)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var caption = captions[reference];
            var icon = reference.IsDirectory
                ? (reference.IsResolved ? FolderIcon : FolderWarningIcon)
                : (reference.IsResolved ? JarIcon : JarWarningIcon);
            var flags = ReferenceFlag + IkvmDependencyTreeFlags.Reference + (reference.IsResolved == false ? ProjectTreeFlags.BrokenReference : ProjectTreeFlags.ResolvedReference);
            var browseObject = await GetBrowseObjectAsync(reference);

            var existing = parent.FindChild(caption, ReferenceFlag);
            parent = existing != null
                ? existing.SetProperties(icon: icon, expandedIcon: icon, flags: flags, browseObjectProperties: browseObject).Parent!
                : parent.Add(context.NewTree(caption, icon, flags, browseObject)).Parent!;
        }

        return parent;
    }

    /// <summary>
    /// Gets the rule shown in the Properties window for a reference.
    /// </summary>
    static async Task<IRule?> GetBrowseObjectAsync(IkvmReference reference)
    {
        var configuredProject = reference.Project;
        var catalog = await configuredProject.Services.PropertyPagesCatalog!.GetCatalogAsync(PropertyPageContexts.BrowseObject);
        var rule = catalog!.BindToContext(IkvmReferenceRules.IkvmReference, configuredProject.UnconfiguredProject.FullPath, IkvmReferenceRules.ItemType, reference.ItemSpec);

        if (reference.ResolvedProperties != null && catalog.GetSchema(IkvmReferenceRules.ResolvedIkvmReference) is { } schema)
            return configuredProject.Services.ExportProvider.GetExportedValue<IRuleFactory>().CreateResolvedReferencePageRule(schema, rule!.Context, reference.ItemSpec, reference.ResolvedProperties);

        return rule;
    }

    /// <summary>
    /// Gets a unique caption for each reference: the file name, or the full item spec when names collide.
    /// </summary>
    static Dictionary<IkvmReference, string> GetCaptions(List<IkvmReference> references)
    {
        var names = references.ToDictionary(i => i, i => i.DisplayName);
        var duplicates = names.Values.GroupBy(i => i, StringComparer.OrdinalIgnoreCase).Where(i => i.Count() > 1).Select(i => i.Key).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        return references.ToDictionary(i => i, i => duplicates.Contains(names[i]) ? i.ItemSpec : names[i]);
    }

}
