using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Reads and changes the <c>IkvmReference</c> elements of the project file.
/// </summary>
[Export]
[AppliesTo(IkvmReferenceCapabilities.IkvmReferences)]
internal sealed class IkvmReferenceWriter
{

    readonly UnconfiguredProject _project;
    readonly IProjectLockService _lockService;

    [ImportingConstructor]
    public IkvmReferenceWriter(UnconfiguredProject project, IProjectLockService lockService)
    {
        _project = project;
        _lockService = lockService;
    }

    /// <summary>
    /// Gets whether the given unevaluated value is free of MSBuild expressions, and unless
    /// <paramref name="isList"/>, of wildcards and lists, so that the UI may safely edit or remove it. Metadata values
    /// are lists: MSBuild expands neither, and the dialog writes several paths separated by semicolons.
    /// </summary>
    public static bool IsLiteral(string? value, bool isList = false)
    {
        if (string.IsNullOrEmpty(value))
            return true;

        return value!.IndexOf("$(", StringComparison.Ordinal) < 0
            && value.IndexOf("@(", StringComparison.Ordinal) < 0
            && value.IndexOf("%(", StringComparison.Ordinal) < 0
            && (isList || value.IndexOfAny(new[] { '*', '?', ';' }) < 0);
    }

    /// <summary>
    /// Reads the project's <c>IkvmReference</c> items, with how MSBuild evaluates them for each of the given configured
    /// projects (one per target framework). Items written in the project file are editable when the UI understands
    /// their element; items imported from other files are read-only.
    /// </summary>
    public async Task<IReadOnlyList<IkvmReferenceElement>> ReadAsync(IReadOnlyCollection<ConfiguredProject>? configuredProjects = null)
    {
        var suggested = await _project.GetSuggestedConfiguredProjectAsync();
        if (suggested == null)
            return Array.Empty<IkvmReferenceElement>();

        var projects = configuredProjects is { Count: > 0 } ? configuredProjects : new[] { suggested };

        try
        {
            return await ReadCoreAsync(projects);
        }
        catch (NotSupportedException)
        {
            // a project loaded from the solution cache has no real elements until it is loaded for real, which a
            // write lock on it does
            await _lockService.WriteLockAsync(async access =>
            {
                await access.GetProjectXmlAsync(_project.FullPath);
                foreach (var configuredProject in projects)
                    await access.GetProjectAsync(configuredProject);
            });

            return await ReadCoreAsync(projects);
        }
    }

    async Task<IReadOnlyList<IkvmReferenceElement>> ReadCoreAsync(IReadOnlyCollection<ConfiguredProject> projects)
    {
        return await _lockService.ReadLockAsync(async access =>
        {
            var evaluated = new List<(ProjectItem Item, string TargetFramework)>();
            foreach (var configuredProject in projects)
            {
                var project = await access.GetProjectAsync(configuredProject);
                configuredProject.ProjectConfiguration.Dimensions.TryGetValue("TargetFramework", out var targetFramework);
                foreach (var item in project.GetItems(IkvmReferenceRules.ItemType))
                    evaluated.Add((item, targetFramework ?? ""));
            }

            // one entry per item: per element, or per evaluated include of an element that produces several items
            // (wildcards, lists); its include and metadata may differ by target framework
            var elements = new ElementIdentities();
            string GetLocation(ProjectItem item) => elements.Get(item.Xml);

            var multiple = new HashSet<string>(evaluated.GroupBy(i => (Location: GetLocation(i.Item), i.TargetFramework)).Where(i => i.Count() > 1).Select(i => i.Key.Location));
            var result = new List<IkvmReferenceElement>();
            foreach (var group in evaluated.GroupBy(i => multiple.Contains(GetLocation(i.Item)) ? GetLocation(i.Item) + "|" + i.Item.EvaluatedInclude : GetLocation(i.Item)))
            {
                var first = group.First().Item;
                var evaluations = group.GroupBy(i => i.TargetFramework).ToDictionary(i => i.Key, i => ToEvaluation(i.First().Item));

                // editable elements are literal: their values by target framework follow from their metadata conditions
                var element = first.IsImported ? null : ToElement(first.Xml);
                if (element is { IsEditable: true })
                {
                    result.Add(element with { Evaluations = evaluations });
                    continue;
                }

                // present in every target framework means all of them
                var targetFrameworks = evaluations.Keys.Where(i => i.Length > 0).ToList();
                if (targetFrameworks.Count == projects.Count)
                    targetFrameworks.Clear();

                var include = multiple.Contains(GetLocation(first)) ? first.EvaluatedInclude : first.UnevaluatedInclude;
                result.Add(new IkvmReferenceElement(include, targetFrameworks, element?.Metadata ?? ToMetadata(first.Xml, out _), false, first.IsImported ? first.Xml.ContainingProject.FullPath : null) { Evaluations = evaluations });
            }

            // the project's own references first, then those imported into it
            return result.OrderBy(i => i.DefinedIn != null).ToList();
        });
    }


    static IkvmReferenceEvaluation ToEvaluation(ProjectItem item) => new(item.EvaluatedInclude, item.DirectMetadata.ToDictionary(i => i.Name, i => i.EvaluatedValue));

    static IkvmReferenceElement ToElement(ProjectItemElement item)
    {
        var metadata = ToMetadata(item, out var isMetadataSupported);
        var isConditionSupported = TargetFrameworkCondition.TryParse(GetGroupCondition(item), out var targetFrameworks);
        var isEditable = isConditionSupported
            && isMetadataSupported
            && string.IsNullOrEmpty(item.Condition)
            && IsLiteral(item.Include)
            && metadata.All(i => IsLiteral(i.Value, isList: true));

        return new IkvmReferenceElement(item.Include, targetFrameworks, metadata, isEditable);
    }

    /// <summary>
    /// Reads the metadata elements of an element; <paramref name="isSupported"/> is <c>false</c> if any has a condition
    /// other than one selecting target frameworks.
    /// </summary>
    static IReadOnlyList<IkvmReferenceMetadata> ToMetadata(ProjectItemElement item, out bool isSupported)
    {
        isSupported = true;
        var result = new List<IkvmReferenceMetadata>();

        foreach (var metadata in item.Metadata)
        {
            if (TargetFrameworkCondition.TryParse(metadata.Condition, out var targetFrameworks) == false)
                isSupported = false;

            result.Add(new IkvmReferenceMetadata(metadata.Name, ProjectCollection.Unescape(metadata.Value), targetFrameworks));
        }

        return result;
    }

    static string GetGroupCondition(ProjectItemElement item)
    {
        return item.Parent is ProjectItemGroupElement group ? group.Condition : "";
    }

    /// <summary>
    /// Applies the given changes to the project file and saves it.
    /// </summary>
    public async Task ApplyAsync(IkvmReferenceChanges changes)
    {
        if (changes.IsEmpty)
            return;

        var configuredProject = await _project.GetSuggestedConfiguredProjectAsync();
        if (configuredProject == null)
            return;

        await _lockService.WriteLockAsync(async access =>
        {
            await access.CheckoutAsync(_project.FullPath);
            var xml = (await access.GetProjectAsync(configuredProject)).Xml;

            foreach (var element in changes.Removed)
                if (FindElement(xml, element) is { } item)
                    RemoveItem(item);

            foreach (var update in changes.Updated)
            {
                if (FindElement(xml, update.Original) is not { } item)
                    continue;

                // moving to another target framework: re-add to the matching group
                if (TargetFrameworkCondition.AreSame(update.Original.TargetFrameworks, update.Updated.TargetFrameworks) == false)
                {
                    RemoveItem(item);
                    AddItem(xml, update.Updated);
                    continue;
                }

                SetMetadata(item, update.Updated.Metadata);
            }

            foreach (var element in changes.Added)
                AddItem(xml, element);
        });

        await _project.SaveAsync();
    }

    /// <summary>
    /// Gets whether each of the given references is defined by an editable element, and so can be removed.
    /// </summary>
    public async Task<bool> CanRemoveAsync(IReadOnlyCollection<IkvmReference> references, IReadOnlyCollection<ConfiguredProject> configuredProjects)
    {
        if (references.Count == 0)
            return false;

        var elements = await ReadAsync(configuredProjects);
        return references.All(r => elements.Any(e => e.IsEditable && string.Equals(e.Include, r.ItemSpec, StringComparison.OrdinalIgnoreCase)));
    }

    /// <summary>
    /// Removes the editable elements behind the given references.
    /// </summary>
    public async Task RemoveAsync(IReadOnlyCollection<IkvmReference> references, IReadOnlyCollection<ConfiguredProject> configuredProjects)
    {
        var elements = await ReadAsync(configuredProjects);
        var removed = elements.Where(e => e.IsEditable && references.Any(r => string.Equals(e.Include, r.ItemSpec, StringComparison.OrdinalIgnoreCase))).ToList();
        await ApplyAsync(new IkvmReferenceChanges(removed, Array.Empty<IkvmReferenceElementUpdate>(), Array.Empty<IkvmReferenceElement>()));
    }

    /// <summary>
    /// Finds the element in the project file matching the given element as read earlier.
    /// </summary>
    static ProjectItemElement? FindElement(ProjectRootElement xml, IkvmReferenceElement element)
    {
        return xml.Items.FirstOrDefault(i =>
            i.ItemType == IkvmReferenceRules.ItemType &&
            string.Equals(i.Include, element.Include, StringComparison.OrdinalIgnoreCase) &&
            TargetFrameworkCondition.TryParse(GetGroupCondition(i), out var targetFrameworks) &&
            TargetFrameworkCondition.AreSame(targetFrameworks, element.TargetFrameworks));
    }

    static void RemoveItem(ProjectItemElement item)
    {
        var group = item.Parent;
        group.RemoveChild(item);

        // drop the group if that emptied it
        if (group is ProjectItemGroupElement itemGroup && itemGroup.Count == 0)
            itemGroup.Parent.RemoveChild(itemGroup);
    }

    static void AddItem(ProjectRootElement xml, IkvmReferenceElement element)
    {
        var item = GetItemGroup(xml, element.TargetFrameworks).AddItem(IkvmReferenceRules.ItemType, ProjectCollection.Escape(element.Include));
        SetMetadata(item, element.Metadata);
    }

    /// <summary>
    /// Makes the metadata written on the element match the given metadata: values for all target frameworks as
    /// attributes, values limited to some as conditioned child elements. Empty values are left out.
    /// </summary>
    static void SetMetadata(ProjectItemElement item, IReadOnlyList<IkvmReferenceMetadata> metadata)
    {
        var values = metadata.Where(i => string.IsNullOrEmpty(i.Value) == false).OrderBy(i => i.TargetFrameworks.Count > 0).ToList();

        foreach (var existing in item.Metadata.ToList())
            item.RemoveChild(existing);

        foreach (var value in values)
        {
            // escape each entry of the list, keeping the separators
            var escaped = string.Join(";", value.Value.Split(';').Select(ProjectCollection.Escape));
            var added = item.AddMetadata(value.Name, escaped, expressAsAttribute: value.TargetFrameworks.Count == 0);
            if (value.TargetFrameworks.Count > 0)
                added.Condition = TargetFrameworkCondition.Format(value.TargetFrameworks);
        }
    }

    /// <summary>
    /// Finds an item group holding references with the condition for the target frameworks, else adds one.
    /// </summary>
    static ProjectItemGroupElement GetItemGroup(ProjectRootElement xml, IReadOnlyList<string> targetFrameworks)
    {
        var existing = xml.ItemGroups.FirstOrDefault(g =>
            TargetFrameworkCondition.TryParse(g.Condition, out var names) &&
            TargetFrameworkCondition.AreSame(names, targetFrameworks) &&
            g.Items.Any(i => i.ItemType == IkvmReferenceRules.ItemType));
        if (existing != null)
            return existing;

        var group = xml.AddItemGroup();
        group.Condition = TargetFrameworkCondition.Format(targetFrameworks);
        return group;
    }

}
