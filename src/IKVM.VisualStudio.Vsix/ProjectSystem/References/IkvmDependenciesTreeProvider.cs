using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using IKVM.VisualStudio.Vsix.Imaging;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Provides the "IKVM Dependencies" node beside Dependencies, listing <c>IkvmReference</c> items, grouped by target
/// framework when the project targets more than one.
/// </summary>
[Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
[Export(typeof(IkvmDependenciesTreeProvider))]
[AppliesTo(IkvmReferenceCapabilities.IkvmReferences)]
internal class IkvmDependenciesTreeProvider : ProjectTreeProviderBase
{

    public const string RootCaption = "IKVM Dependencies";

    public static readonly ProjectTreeFlags RootFlag = ProjectTreeFlags.Create("IkvmDependenciesRoot");
    public static readonly ProjectTreeFlags TargetFrameworkFlag = ProjectTreeFlags.Create("IkvmDependenciesTargetFramework");
    public static readonly ProjectTreeFlags ReferenceFlag = ProjectTreeFlags.Create("IkvmReference");

    static readonly ProjectImageMoniker RootIcon = IkvmMonikers.IkvmDependencies.ToProjectSystemType();
    static readonly ProjectImageMoniker TargetFrameworkIcon = KnownMonikers.Library.ToProjectSystemType();
    static readonly ProjectImageMoniker JarIcon = IkvmMonikers.JarFile.ToProjectSystemType();
    static readonly ProjectImageMoniker JarWarningIcon = IkvmMonikers.JarFileWarning.ToProjectSystemType();
    static readonly ProjectImageMoniker FolderIcon = IkvmMonikers.ClassFolder.ToProjectSystemType();
    static readonly ProjectImageMoniker FolderWarningIcon = IkvmMonikers.ClassFolderWarning.ToProjectSystemType();

    readonly IActiveConfigurationGroupService _configurationGroupService;
    readonly object _sync = new object();

    IDisposable? _groupLink;
    ImmutableDictionary<ConfiguredProject, ProjectState> _projects = ImmutableDictionary<ConfiguredProject, ProjectState>.Empty;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    [ImportingConstructor]
    public IkvmDependenciesTreeProvider(
        IProjectThreadingService threadingService,
        UnconfiguredProject unconfiguredProject,
        IActiveConfigurationGroupService configurationGroupService) :
        base(threadingService, unconfiguredProject, false)
    {
        _configurationGroupService = configurationGroupService;
    }

    protected override void Initialize()
    {
        base.Initialize();

        _ = SubmitTreeUpdateAsync((treeSnapshot, exports, cancellationToken) =>
            Task.FromResult(new TreeUpdateResult(CreateRoot(), null)));

        var target = new ActionBlock<IProjectVersionedValue<IConfigurationGroup<ConfiguredProject>>>(OnConfigurationGroupChanged);
        _groupLink = _configurationGroupService.ActiveConfiguredProjectGroupSource.SourceBlock.LinkTo(target, new DataflowLinkOptions() { PropagateCompletion = true });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _groupLink?.Dispose();

            lock (_sync)
            {
                foreach (var state in _projects.Values)
                    state.Link.Dispose();

                _projects = _projects.Clear();
            }
        }

        base.Dispose(disposing);
    }

    protected override ConfiguredProjectExports GetActiveConfiguredProjectExports(ConfiguredProject newActiveConfiguredProject)
    {
        return GetActiveConfiguredProjectExports<IkvmDependenciesConfiguredProjectExports>(newActiveConfiguredProject);
    }

    /// <summary>
    /// Gets the IKVM references currently known for each configured project of the active configuration group.
    /// </summary>
    public ImmutableArray<IkvmReference> GetReferences()
    {
        lock (_sync)
            return _projects.Values.SelectMany(i => i.References).ToImmutableArray();
    }

    /// <summary>
    /// Gets the configured projects of the active configuration group, one per target framework.
    /// </summary>
    public ImmutableArray<ConfiguredProject> GetConfiguredProjects()
    {
        lock (_sync)
            return _projects.Keys.ToImmutableArray();
    }

    /// <summary>
    /// Gets the target frameworks of the active configuration group, if the project targets more than one.
    /// </summary>
    public ImmutableArray<string> GetTargetFrameworks()
    {
        lock (_sync)
        {
            var names = _projects.Keys.Select(GetTargetFramework).Where(i => i.Length > 0).OrderBy(i => i, StringComparer.OrdinalIgnoreCase).ToImmutableArray();
            return names.Length > 1 ? names : ImmutableArray<string>.Empty;
        }
    }

    /// <summary>
    /// Gets the reference shown by the given node.
    /// </summary>
    public bool TryGetReference(IProjectTree node, out IkvmReference? reference)
    {
        reference = null;
        if (node.Flags.Contains(ReferenceFlag) == false)
            return false;

        var targetFramework = node.Parent != null && node.Parent.Flags.Contains(TargetFrameworkFlag) ? node.Parent.Caption : null;

        List<IkvmReference> candidates;
        lock (_sync)
            candidates = _projects
                .Where(i => targetFramework == null || string.Equals(GetTargetFramework(i.Key), targetFramework, StringComparison.OrdinalIgnoreCase))
                .SelectMany(i => i.Value.References)
                .ToList();

        var captions = GetCaptions(candidates);
        reference = candidates.FirstOrDefault(i => string.Equals(captions[i], node.Caption, StringComparison.OrdinalIgnoreCase));
        return reference != null;
    }

    /// <summary>
    /// Subscribes to the rule data of each configured project in the active configuration group.
    /// </summary>
    void OnConfigurationGroupChanged(IProjectVersionedValue<IConfigurationGroup<ConfiguredProject>> group)
    {
        lock (_sync)
        {
            var current = group.Value.ToImmutableHashSet();

            foreach (var removed in _projects.Keys.Where(i => current.Contains(i) == false).ToList())
            {
                _projects[removed].Link.Dispose();
                _projects = _projects.Remove(removed);
            }

            foreach (var configuredProject in current.Where(i => _projects.ContainsKey(i) == false))
            {
                var target = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(update => OnProjectUpdated(configuredProject, update));
                var link = configuredProject.Services.ProjectSubscription!.JointRuleSource.SourceBlock.LinkTo(
                    target,
                    new DataflowLinkOptions() { PropagateCompletion = true },
                    initialDataAsNew: true,
                    suppressVersionOnlyUpdates: true,
                    IkvmReferenceRules.IkvmReference,
                    IkvmReferenceRules.ResolvedIkvmReference);

                _projects = _projects.Add(configuredProject, new ProjectState(link, ImmutableArray<IkvmReference>.Empty));
            }
        }

        ScheduleTreeUpdate();
    }

    /// <summary>
    /// Records the latest rule data for a configured project.
    /// </summary>
    void OnProjectUpdated(ConfiguredProject configuredProject, IProjectVersionedValue<IProjectSubscriptionUpdate> update)
    {
        var references = IkvmReference.Create(configuredProject, update.Value);

        lock (_sync)
        {
            if (_projects.TryGetValue(configuredProject, out var state) == false)
                return;

            _projects = _projects.SetItem(configuredProject, state with { References = references });
        }

        ScheduleTreeUpdate();
    }

    void ScheduleTreeUpdate()
    {
        _ = SubmitTreeUpdateAsync(async (treeSnapshot, exports, cancellationToken) =>
        {
            ImmutableDictionary<ConfiguredProject, ProjectState> projects;
            lock (_sync)
                projects = _projects;

            var root = await UpdateRootAsync(treeSnapshot?.Value?.Tree ?? CreateRoot(), projects, cancellationToken);
            return new TreeUpdateResult(root, null);
        });
    }

    IProjectTree CreateRoot()
    {
        return NewTree(RootCaption, icon: RootIcon, expandedIcon: RootIcon, flags: RootFlag.Union(ProjectTreeFlags.Create(ProjectTreeFlags.Common.VirtualFolder | ProjectTreeFlags.Common.BubbleUp)));
    }

    /// <summary>
    /// Updates the root node to reflect the given configured projects.
    /// </summary>
    async Task<IProjectTree> UpdateRootAsync(IProjectTree root, ImmutableDictionary<ConfiguredProject, ProjectState> projects, CancellationToken cancellationToken)
    {
        var groups = projects
            .Select(i => (Name: GetTargetFramework(i.Key), Project: i.Key, i.Value.References))
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groups.Count <= 1)
        {
            // single target: references directly under the root
            root = RemoveChildren(root, i => i.Flags.Contains(TargetFrameworkFlag));
            return await UpdateReferencesAsync(root, groups.SelectMany(i => i.References), cancellationToken);
        }

        // multiple targets: a folder per target framework
        var names = groups.Select(i => i.Name).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        root = RemoveChildren(root, i => i.Flags.Contains(TargetFrameworkFlag) == false || names.Contains(i.Caption) == false);

        foreach (var group in groups)
        {
            var folder = FindChild(root, group.Name, TargetFrameworkFlag);
            folder ??= root.Add(NewTree(group.Name, icon: TargetFrameworkIcon, expandedIcon: TargetFrameworkIcon, flags: TargetFrameworkFlag.Union(ProjectTreeFlags.Create(ProjectTreeFlags.Common.VirtualFolder))));
            folder = await UpdateReferencesAsync(folder, group.References, cancellationToken);
            root = folder.Parent!;
        }

        return root;
    }

    /// <summary>
    /// Updates the reference children of the given node, reusing existing nodes so that their state is kept.
    /// </summary>
    async Task<IProjectTree> UpdateReferencesAsync(IProjectTree parent, IEnumerable<IkvmReference> references, CancellationToken cancellationToken)
    {
        var list = references.ToList();
        var captions = GetCaptions(list);
        parent = RemoveChildren(parent, i => i.Flags.Contains(ReferenceFlag) && captions.Values.Contains(i.Caption) == false);

        foreach (var reference in list)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var caption = captions[reference];
            var icon = reference.IsDirectory
                ? (reference.IsResolved ? FolderIcon : FolderWarningIcon)
                : (reference.IsResolved ? JarIcon : JarWarningIcon);
            var flags = ReferenceFlag + (reference.IsResolved == false ? ProjectTreeFlags.BrokenReference : ProjectTreeFlags.ResolvedReference);
            var browseObject = await GetBrowseObjectAsync(reference);

            var existing = FindChild(parent, caption, ReferenceFlag);
            if (existing != null)
                parent = existing.SetProperties(icon: icon, expandedIcon: icon, flags: flags, browseObjectProperties: browseObject).Parent!;
            else
                parent = parent.Add(NewTree(caption, icon: icon, expandedIcon: icon, flags: flags, browseObjectProperties: browseObject)).Parent!;
        }

        return parent;
    }

    /// <summary>
    /// Gets the rule shown in the Properties window for a reference.
    /// </summary>
    async Task<IRule?> GetBrowseObjectAsync(IkvmReference reference)
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

    static string GetTargetFramework(ConfiguredProject configuredProject)
    {
        return configuredProject.ProjectConfiguration.Dimensions.TryGetValue("TargetFramework", out var targetFramework) ? targetFramework : "";
    }

    static IProjectTree? FindChild(IProjectTree parent, string caption, ProjectTreeFlags flag)
    {
        return parent.Children.FirstOrDefault(i => i.Flags.Contains(flag) && string.Equals(i.Caption, caption, StringComparison.OrdinalIgnoreCase));
    }

    static IProjectTree RemoveChildren(IProjectTree parent, Func<IProjectTree, bool> predicate)
    {
        foreach (var child in parent.Children.Where(predicate).ToList())
            parent = parent.Remove(parent.Children.First(i => i.Identity == child.Identity));

        return parent;
    }

    sealed record ProjectState(IDisposable Link, ImmutableArray<IkvmReference> References);

    [Export]
    protected class IkvmDependenciesConfiguredProjectExports : ConfiguredProjectExports
    {

        [ImportingConstructor]
        public IkvmDependenciesConfiguredProjectExports(ConfiguredProject configuredProject) :
            base(configuredProject)
        {

        }

    }

}
