using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows;

using IKVM.VisualStudio.Maven.Imaging;
using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.ProjectSystem.UI;

using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// Supplies the <c>MavenReference</c> items of a project using IKVM.Maven.Sdk to the Manage IKVM Dependencies dialog.
/// </summary>
[Export(typeof(IkvmDependencyEntryProvider))]
[AppliesTo(MavenReferenceRules.Capability)]
[Order(900)]
internal sealed class MavenReferenceEntryProvider : IkvmDependencyEntryProvider
{

    /// <summary>
    /// How long to wait for what Maven resolved, which needs a design-time build.
    /// </summary>
    static readonly TimeSpan ResolveTimeout = TimeSpan.FromSeconds(60);

    public override string ItemType => MavenReferenceRules.ItemType;

    public override IkvmDependencyEntry CreateEntry(IkvmDependencyEntryContext context, IkvmDependencyElement element)
    {
        return MavenDependencyEntry.FromElement(context, element);
    }

    public override IReadOnlyList<IkvmDependencyAddCommand> GetAddCommands(IkvmDependencyEntryContext context)
    {
        return new[] { new IkvmDependencyAddCommand("Maven...", MavenMonikers.MavenReference, "Add Maven reference", owner => Task.FromResult(Add(context, owner))) };
    }

    static IReadOnlyList<IkvmDependencyEntry> Add(IkvmDependencyEntryContext context, Window owner)
    {
        var dialog = new AddMavenReferenceDialog() { Owner = owner };
        if (dialog.ShowModal() != true || MavenCoordinates.TryParseInclude(dialog.Coordinates, out var groupId, out var artifactId, out var version) == false)
            return Array.Empty<IkvmDependencyEntry>();

        // one reference per group and artifact
        if (context.Entries.OfType<MavenDependencyEntry>().Any(i => string.Equals(i.GroupId, groupId, StringComparison.OrdinalIgnoreCase) && string.Equals(i.ArtifactId, artifactId, StringComparison.OrdinalIgnoreCase)))
            return Array.Empty<IkvmDependencyEntry>();

        return new[] { MavenDependencyEntry.ForNew(context, groupId, artifactId, version ?? "") };
    }

    /// <summary>
    /// Gives each entry what Maven resolved for it in the last design-time build of each target framework.
    /// </summary>
    public override async Task LoadAsync(IkvmDependencyEntryContext context, IReadOnlyList<IkvmDependencyEntry> entries, CancellationToken cancellationToken)
    {
        var maven = entries.OfType<MavenDependencyEntry>().ToList();
        if (maven.Count == 0)
            return;

        var references = new Dictionary<string, ImmutableArray<MavenReference>>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in context.ConfiguredProjects)
        {
            var rules = await GetRulesAsync(item.Value, cancellationToken);
            references[item.Key] = rules != null ? MavenReference.Create(item.Value, rules) : ImmutableArray<MavenReference>.Empty;
        }

        foreach (var entry in maven)
        {
            var resolved = new Dictionary<string, MavenReference?>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in references)
                resolved[item.Key] = item.Value.FirstOrDefault(i =>
                    string.Equals(i.GroupId, entry.GroupId, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(i.ArtifactId, entry.ArtifactId, StringComparison.OrdinalIgnoreCase));

            entry.SetResolved(resolved);
        }
    }

    /// <summary>
    /// Gets the latest snapshots of the Maven rules of a configured project, or <c>null</c> if they do not come in time.
    /// </summary>
    static async Task<IImmutableDictionary<string, IProjectRuleSnapshot>?> GetRulesAsync(ConfiguredProject project, CancellationToken cancellationToken)
    {
        var result = new TaskCompletionSource<IImmutableDictionary<string, IProjectRuleSnapshot>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var target = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(update => result.TrySetResult(update.Value.CurrentState));

        using var link = project.Services.ProjectSubscription!.JointRuleSource.SourceBlock.LinkTo(
            target,
            new DataflowLinkOptions(),
            initialDataAsNew: true,
            suppressVersionOnlyUpdates: true,
            MavenReferenceRules.MavenReference,
            MavenReferenceRules.ResolvedMavenReference);

        var completed = await Task.WhenAny(result.Task, Task.Delay(ResolveTimeout, cancellationToken));
        return completed == result.Task ? result.Task.Result : null;
    }

}
