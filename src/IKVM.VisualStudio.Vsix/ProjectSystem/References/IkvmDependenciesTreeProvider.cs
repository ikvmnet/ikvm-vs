using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.Vsix.Imaging;

using Microsoft.VisualStudio.Imaging;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Provides the "IKVM Dependencies" node beside Dependencies, grouped by target framework when the project targets
/// more than one. The dependencies themselves come from the exported <see cref="IIkvmDependencyTreeProvider"/>s that
/// apply to each configured project.
/// </summary>
[Export(ExportContractNames.ProjectTreeProviders.PhysicalViewRootGraft, typeof(IProjectTreeProvider))]
[Export(typeof(IkvmDependenciesTreeProvider))]
[AppliesTo(IkvmDependencyCapabilities.IkvmReferences)]
internal class IkvmDependenciesTreeProvider : ProjectTreeProviderBase
{

    public const string RootCaption = "IKVM Dependencies";

    static readonly ProjectImageMoniker RootIcon = IkvmMonikers.IkvmDependencies.ToProjectSystemType();
    static readonly ProjectImageMoniker TargetFrameworkIcon = KnownMonikers.Library.ToProjectSystemType();
    static readonly ProjectImageMoniker JarIcon = IkvmMonikers.JarFile.ToProjectSystemType();
    static readonly ProjectImageMoniker JarWarningIcon = IkvmMonikers.JarFileWarning.ToProjectSystemType();
    static readonly ProjectImageMoniker FolderIcon = IkvmMonikers.ClassFolder.ToProjectSystemType();

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

    /// <summary>
    /// Gets the providers of dependency nodes, filtered for each configured project by their capabilities.
    /// </summary>
    [ImportMany]
    internal IEnumerable<Lazy<IIkvmDependencyTreeProvider, IOrderPrecedenceMetadataView>> Providers { get; set; } = null!;

    /// <summary>
    /// Publishes the empty root node, then follows the active configuration group to learn which configured projects
    /// to show dependencies for.
    /// </summary>
    protected override void Initialize()
    {
        base.Initialize();

        _ = SubmitTreeUpdateAsync((treeSnapshot, exports, cancellationToken) =>
            Task.FromResult(new TreeUpdateResult(CreateRoot(), null)));

        var target = new ActionBlock<IProjectVersionedValue<IConfigurationGroup<ConfiguredProject>>>(OnConfigurationGroupChanged);
        _groupLink = _configurationGroupService.ActiveConfiguredProjectGroupSource.SourceBlock.LinkTo(target, new DataflowLinkOptions() { PropagateCompletion = true });
    }

    /// <summary>
    /// Unlinks from the configuration group and from the rule subscriptions of every configured project.
    /// </summary>
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

    /// <inheritdoc />
    protected override ConfiguredProjectExports GetActiveConfiguredProjectExports(ConfiguredProject newActiveConfiguredProject)
    {
        return GetActiveConfiguredProjectExports<IkvmDependenciesConfiguredProjectExports>(newActiveConfiguredProject);
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
    /// Subscribes to the rules of the applicable providers, for each configured project in the active configuration
    /// group.
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
                // the rules of a provider may exist only in projects with its capability
                var providers = Providers
                    .Where(i => configuredProject.Capabilities.AppliesTo(i.Metadata.AppliesTo))
                    .OrderByDescending(i => i.Metadata.OrderPrecedence)
                    .Select(i => i.Value)
                    .ToImmutableArray();
                var rules = providers.SelectMany(i => i.RuleNames).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();

                var target = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(update => OnProjectUpdated(configuredProject, update));
                var link = configuredProject.Services.ProjectSubscription!.JointRuleSource.SourceBlock.LinkTo(
                    target,
                    new DataflowLinkOptions() { PropagateCompletion = true },
                    initialDataAsNew: true,
                    suppressVersionOnlyUpdates: true,
                    rules);

                _projects = _projects.Add(configuredProject, new ProjectState(link, providers, null));
            }
        }

        ScheduleTreeUpdate();
    }

    /// <summary>
    /// Records the latest rule data for a configured project.
    /// </summary>
    void OnProjectUpdated(ConfiguredProject configuredProject, IProjectVersionedValue<IProjectSubscriptionUpdate> update)
    {
        lock (_sync)
        {
            if (_projects.TryGetValue(configuredProject, out var state) == false)
                return;

            _projects = _projects.SetItem(configuredProject, state with { Rules = update.Value.CurrentState });
        }

        ScheduleTreeUpdate();
    }

    /// <summary>
    /// Queues a rebuild of the tree from a snapshot of the current project states.
    /// </summary>
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

    /// <summary>
    /// Creates the empty IKVM Dependencies node, a virtual folder that bubbles up beside Dependencies.
    /// </summary>
    IProjectTree CreateRoot()
    {
        return NewTree(RootCaption, icon: RootIcon, expandedIcon: RootIcon, flags: IkvmDependencyTreeFlags.Root.Union(ProjectTreeFlags.Create(ProjectTreeFlags.Common.VirtualFolder | ProjectTreeFlags.Common.BubbleUp)));
    }

    /// <summary>
    /// Updates the root node to reflect the given configured projects.
    /// </summary>
    async Task<IProjectTree> UpdateRootAsync(IProjectTree root, ImmutableDictionary<ConfiguredProject, ProjectState> projects, CancellationToken cancellationToken)
    {
        var groups = projects
            .Select(i => (Name: GetTargetFramework(i.Key), Project: i.Key, State: i.Value))
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (groups.Count <= 1)
        {
            // single target: dependencies directly under the root
            root = root.RemoveChildren(i => i.Flags.Contains(IkvmDependencyTreeFlags.TargetFramework));
            foreach (var group in groups)
                root = await UpdateDependenciesAsync(root, group.Project, "", group.State, cancellationToken);

            return root;
        }

        // multiple targets: a folder per target framework, holding nothing else
        var names = groups.Select(i => i.Name).ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
        root = root.RemoveChildren(i => i.Flags.Contains(IkvmDependencyTreeFlags.TargetFramework) == false || names.Contains(i.Caption) == false);

        foreach (var group in groups)
        {
            var folder = root.FindChild(group.Name, IkvmDependencyTreeFlags.TargetFramework);
            folder ??= root.Add(NewTree(group.Name, icon: TargetFrameworkIcon, expandedIcon: TargetFrameworkIcon, flags: IkvmDependencyTreeFlags.TargetFramework.Union(ProjectTreeFlags.Create(ProjectTreeFlags.Common.VirtualFolder))));
            folder = await UpdateDependenciesAsync(folder, group.Project, group.Name, group.State, cancellationToken);
            root = folder.Parent!;
        }

        return root;
    }

    /// <summary>
    /// Lets each provider applicable to a configured project update its nodes under the given parent.
    /// </summary>
    async Task<IProjectTree> UpdateDependenciesAsync(IProjectTree parent, ConfiguredProject configuredProject, string targetFramework, ProjectState state, CancellationToken cancellationToken)
    {
        // no data yet: keep what is shown
        if (state.Rules == null)
            return parent;

        foreach (var provider in state.Providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var rules = provider.RuleNames
                .Where(state.Rules.ContainsKey)
                .ToImmutableDictionary(i => i, i => state.Rules[i], StringComparer.OrdinalIgnoreCase);

            try
            {
                parent = await provider.UpdateTreeAsync(new TreeContext(this, configuredProject, targetFramework, rules), parent, cancellationToken);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // a failing provider does not take the others down
                ActivityLog.TryLogError(nameof(IkvmDependenciesTreeProvider), $"{provider.GetType().FullName} could not update the tree: {e}");
            }
        }

        return parent;
    }

    /// <summary>
    /// Gets the <c>TargetFramework</c> dimension of a configured project, or an empty string if it has none.
    /// </summary>
    static string GetTargetFramework(ConfiguredProject configuredProject)
    {
        return configuredProject.ProjectConfiguration.Dimensions.TryGetValue("TargetFramework", out var targetFramework) ? targetFramework : "";
    }

    /// <summary>
    /// The rule subscription of a configured project, the providers that apply to it, and its latest rule data.
    /// </summary>
    sealed record ProjectState(IDisposable Link, ImmutableArray<IIkvmDependencyTreeProvider> Providers, IImmutableDictionary<string, IProjectRuleSnapshot>? Rules);

    /// <summary>
    /// What a provider gets for one configured project.
    /// </summary>
    sealed class TreeContext : IIkvmDependencyTreeContext
    {

        readonly IkvmDependenciesTreeProvider _owner;

        /// <summary>
        /// Initializes a new instance, creating nodes through <paramref name="owner"/>.
        /// </summary>
        public TreeContext(IkvmDependenciesTreeProvider owner, ConfiguredProject configuredProject, string targetFramework, IImmutableDictionary<string, IProjectRuleSnapshot> rules)
        {
            _owner = owner;
            ConfiguredProject = configuredProject;
            TargetFramework = targetFramework;
            Rules = rules;
        }

        /// <inheritdoc />
        public ConfiguredProject ConfiguredProject { get; }

        /// <summary>
        /// Target framework of the configured project, or empty when the project targets only one.
        /// </summary>
        public string TargetFramework { get; }

        /// <summary>
        /// Snapshots of the rules the provider asked for, limited to those the configured project has.
        /// </summary>
        public IImmutableDictionary<string, IProjectRuleSnapshot> Rules { get; }

        /// <inheritdoc />
        public IProjectTree NewTree(string caption, ProjectImageMoniker icon, ProjectTreeFlags flags, IRule? browseObject = null, ProjectImageMoniker? expandedIcon = null)
        {
            return _owner.NewTree(caption, icon: icon, expandedIcon: expandedIcon ?? icon, flags: flags, browseObjectProperties: browseObject);
        }

        /// <summary>
        /// Creates a node for a JAR file or class directory on disk, with an icon reflecting which it is or whether it
        /// is missing, and the file's properties as its browse object.
        /// </summary>
        public IProjectTree NewJarFileTree(string fullPath, ProjectTreeFlags flags)
        {
            var icon = Directory.Exists(fullPath) ? FolderIcon : File.Exists(fullPath) ? JarIcon : JarWarningIcon;
            var browseObject = JarFileBrowseObject.Create(ConfiguredProject, fullPath);
            return NewTree(Path.GetFileName(fullPath.TrimEnd('\\', '/')), icon, flags + IkvmDependencyTreeFlags.JarFile, browseObject);
        }

    }

    /// <summary>
    /// Services of the active configured project that the tree provider base class needs.
    /// </summary>
    [Export]
    protected class IkvmDependenciesConfiguredProjectExports : ConfiguredProjectExports
    {

        /// <summary>
        /// Initializes a new instance for the configured project.
        /// </summary>
        [ImportingConstructor]
        public IkvmDependenciesConfiguredProjectExports(ConfiguredProject configuredProject) :
            base(configuredProject)
        {

        }

    }

}
