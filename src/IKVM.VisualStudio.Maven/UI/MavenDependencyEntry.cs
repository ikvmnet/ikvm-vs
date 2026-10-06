using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

using IKVM.VisualStudio.Maven.Imaging;
using IKVM.VisualStudio.ProjectSystem;
using IKVM.VisualStudio.ProjectSystem.UI;

using Microsoft.VisualStudio.Imaging.Interop;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// A <c>MavenReference</c> item in the Manage IKVM Dependencies dialog: one already in the project, or one being
/// added. Its group and artifact are its identity; its other settings can differ by target framework.
/// </summary>
sealed class MavenDependencyEntry : IkvmDependencyEntry<MavenDependencyValues>
{

    /// <summary>
    /// The scopes Maven knows, with the default first.
    /// </summary>
    public static IReadOnlyList<string> Scopes { get; } = new[] { "", "compile", "provided", "runtime", "test", "system" };

    /// <summary>
    /// Creates an entry for an item already in the project.
    /// </summary>
    public static MavenDependencyEntry FromElement(IkvmDependencyEntryContext context, IkvmDependencyElement element)
    {
        var entry = new MavenDependencyEntry(context, element, "", "");

        foreach (var key in entry.Keys)
        {
            // coordinates come from metadata, else from an include of the form groupId:artifactId[:version]
            MavenCoordinates.TryParseInclude(entry.GetOriginalInclude(key), out var groupId, out var artifactId, out var version);
            if (entry.GroupId.Length == 0)
                entry.GroupId = entry.GetOriginalMetadata(MavenReferenceRules.GroupIdMetadata, key) is { Length: > 0 } g ? g : groupId;
            if (entry.ArtifactId.Length == 0)
                entry.ArtifactId = entry.GetOriginalMetadata(MavenReferenceRules.ArtifactIdMetadata, key) is { Length: > 0 } a ? a : artifactId;

            var values = entry.GetValues(key);
            values.Version = entry.GetOriginalMetadata(MavenReferenceRules.VersionMetadata, key) is { Length: > 0 } v ? v : version ?? "";
            values.Classifier = entry.GetOriginalMetadata(MavenReferenceRules.ClassifierMetadata, key);
            values.Scope = entry.GetOriginalMetadata(MavenReferenceRules.ScopeMetadata, key).Trim();
            values.Optional = string.Equals(entry.GetOriginalMetadata(MavenReferenceRules.OptionalMetadata, key).Trim(), "true", StringComparison.OrdinalIgnoreCase);
            values.Exclusions.AddRange(Split(entry.GetOriginalMetadata(MavenReferenceRules.ExclusionsMetadata, key)));
        }

        entry.Refresh();
        return entry;
    }

    /// <summary>
    /// Creates an entry for a reference being added.
    /// </summary>
    public static MavenDependencyEntry ForNew(IkvmDependencyEntryContext context, string groupId, string artifactId, string version, string scope = "")
    {
        var entry = new MavenDependencyEntry(context, null, groupId, artifactId);
        foreach (var key in entry.Keys)
        {
            entry.GetValues(key).Version = version;
            entry.GetValues(key).Scope = scope;
        }

        entry.Refresh();
        return entry;
    }

    static IEnumerable<string> Split(string value) => value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()).Where(i => i.Length > 0);

    bool _isLoaded;

    MavenDependencyEntry(IkvmDependencyEntryContext context, IkvmDependencyElement? original, string groupId, string artifactId) :
        base(context, MavenReferenceRules.ItemType, original)
    {
        GroupId = groupId;
        ArtifactId = artifactId;
    }

    protected override FrameworkElement CreateView() => new MavenDependencyView() { DataContext = this };

    public string GroupId { get; private set; }

    public string ArtifactId { get; private set; }

    /// <summary>
    /// The coordinates for the target frameworks being edited, with their version when they agree on one.
    /// </summary>
    public string Coordinates => MavenCoordinates.Format(GroupId, ArtifactId, Common(i => i.Classifier), Common(i => i.Version));

    public override string DisplayName => Coordinates;

    public override string? Subtitle => Common(i => i.Scope) is { Length: > 0 } scope ? $"Maven, {scope} scope" : "Maven";

    public override string? Location => ResolvedVersion is { } resolved ? $"Resolved {MavenCoordinates.Format(GroupId, ArtifactId, Common(i => i.Classifier), resolved)}" : _isLoaded ? "Not resolved" : "Resolving...";

    /// <summary>
    /// The version Maven resolved for the target frameworks being edited, when they agree on one.
    /// </summary>
    string? ResolvedVersion => ShownValues.Select(i => i.Resolved?.Artifact?.Version).Distinct().ToList() is { Count: 1 } versions ? versions[0] : null;

    public override ImageMoniker Icon => _isLoaded && IsEditable && ShownValues.Any(i => i.Resolved?.IsResolved != true) ? MavenMonikers.MavenReferenceWarning : MavenMonikers.MavenReference;

    // settings

    public string Version
    {
        get => Common(i => i.Version);
        set => Apply(i => i.Version = value.Trim());
    }

    public string? VersionVariations => Variations(i => i.Version);

    public string VersionPlaceholder => Varies(i => i.Version) ? VariesPlaceholder : "Required, such as 1.2.3 or [1.0,2.0)";

    public FieldState VersionFrame => GetFieldState(IsInvalid(i => i.Version.Length > 0), Varies(i => i.Version));

    public string Classifier
    {
        get => Common(i => i.Classifier);
        set => Apply(i => i.Classifier = value.Trim());
    }

    public string? ClassifierVariations => Variations(i => i.Classifier);

    public string ClassifierPlaceholder => Varies(i => i.Classifier) ? VariesPlaceholder : "None";

    public FieldState ClassifierFrame => GetFieldState(false, Varies(i => i.Classifier));

    public IReadOnlyList<string> ScopeOptions => Scopes;

    /// <summary>
    /// The scope for the target frameworks being edited, or <c>null</c> when they differ.
    /// </summary>
    public string? Scope
    {
        get => Varies(i => i.Scope) ? null : Common(i => i.Scope);
        set
        {
            if (value != null)
                Apply(i => i.Scope = value);
        }
    }

    public string? ScopeVariations => Variations(i => i.Scope.Length > 0 ? i.Scope : "compile");

    public FieldState ScopeFrame => GetFieldState(IsInvalid(i => Scopes.Contains(i.Scope)), Varies(i => i.Scope));

    public bool? Optional
    {
        get => Common(i => i.Optional);
        set
        {
            if (value is { } optional)
                Apply(i => i.Optional = optional);
        }
    }

    public string OptionalTip => Variations(i => i.Optional ? "true" : "false") ?? "Not passed on to projects that depend on this one.";

    // exclusions

    /// <summary>
    /// The exclusions for the target frameworks being edited.
    /// </summary>
    public IReadOnlyList<MavenExclusionItem> ExclusionItems => ShownValues
        .SelectMany(i => i.Exclusions)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Select(exclusion =>
        {
            var keys = ShownKeys.Where(i => GetValues(i).Exclusions.Contains(exclusion, StringComparer.OrdinalIgnoreCase)).ToList();
            return new MavenExclusionItem(exclusion, keys.Count < ShownKeys.Count ? "Only for " + string.Join(", ", keys) : null, IsExclusionValid(exclusion));
        })
        .ToList();

    public FieldState ExclusionsFrame => GetFieldState(IsInvalid(i => i.Exclusions.All(IsExclusionValid)), Varies(i => string.Join(";", i.Exclusions)));

    public void AddExclusion(string exclusion)
    {
        exclusion = exclusion.Trim();
        if (exclusion.Length == 0)
            return;

        Apply(i =>
        {
            if (i.Exclusions.Contains(exclusion, StringComparer.OrdinalIgnoreCase) == false)
                i.Exclusions.Add(exclusion);
        });
    }

    public void RemoveExclusion(string exclusion)
    {
        Apply(i => i.Exclusions.RemoveAll(j => string.Equals(j, exclusion, StringComparison.OrdinalIgnoreCase)));
    }

    static bool IsExclusionValid(string exclusion) => exclusion.Split(':') is { Length: >= 2 and <= 4 } parts && parts.All(i => i.Trim().Length > 0);

    // resolved dependencies

    /// <summary>
    /// The artifacts Maven resolved for the reference, for the target frameworks being edited: the first one's tree,
    /// followed by any the others add.
    /// </summary>
    public IReadOnlyList<MavenResolvedItem> ResolvedItems
    {
        get
        {
            var order = new List<(MavenArtifact Artifact, int Depth)>();
            foreach (var values in ShownValues)
                if (values.Resolved is { Artifact: { } root } reference)
                    Visit(reference, root, 1, new HashSet<string>(StringComparer.OrdinalIgnoreCase) { root.ItemSpec });

            return order.Select(i =>
            {
                var keys = ShownKeys.Where(k => GetValues(k).Resolved?.Graph?.Values.Any(a => string.Equals(a.Coordinates, i.Artifact.Coordinates, StringComparison.OrdinalIgnoreCase)) == true).ToList();
                return new MavenResolvedItem(i.Artifact, i.Depth, keys.Count < ShownKeys.Count ? "Only for " + string.Join(", ", keys) : null, false);
            }).ToList();

            void Visit(MavenReference reference, MavenArtifact artifact, int depth, HashSet<string> path)
            {
                foreach (var dependency in reference.GetDependencies(artifact))
                {
                    if (path.Contains(dependency.ItemSpec))
                        continue;

                    if (order.Any(i => string.Equals(i.Artifact.Coordinates, dependency.Coordinates, StringComparison.OrdinalIgnoreCase)) == false)
                        order.Add((dependency, depth));

                    path.Add(dependency.ItemSpec);
                    Visit(reference, dependency, depth + 1, path);
                    path.Remove(dependency.ItemSpec);
                }
            }
        }
    }

    public bool HasResolvedItems => ResolvedItems.Count > 0;

    public string ResolvedNote => _isLoaded == false ? "Resolving..." : ShownValues.Any(i => i.Resolved?.IsResolved == true) ? "Changes show here once the project is saved and built." : "Not resolved yet: save, then build the project.";

    /// <summary>
    /// Takes what Maven resolved for the reference in each target framework.
    /// </summary>
    internal void SetResolved(IReadOnlyDictionary<string, MavenReference?> resolved)
    {
        foreach (var key in Keys)
            GetValues(key).Resolved = resolved.TryGetValue(key, out var reference) ? reference : null;

        _isLoaded = true;
        Refresh();
    }

    public override IReadOnlyList<IkvmDependencyMenuItem> GetMenuItems()
    {
        return new[]
        {
            new IkvmDependencyMenuItem("_Copy Coordinates", () => Clipboard.SetText(Coordinates)),
        };
    }

    public override string? Status => base.Status ?? (_isLoaded && IsEditable && IsNew == false && ShownValues.Any(i => i.Resolved?.IsResolved != true) ? "Maven did not resolve this reference in its last build. Check the version and the Maven repositories of the project." : null);

    // validation and saving

    protected override void Validate(IkvmDependencyValidation validation)
    {
        if (GroupId.Length == 0 || ArtifactId.Length == 0)
            validation.AddError("The group and artifact of the reference are not known.");

        AddError(validation, i => i.Version.Length > 0, "A version is required.");
        AddError(validation, i => Scopes.Contains(i.Scope), "The scope is not one Maven knows: compile, provided, runtime, test or system.");
        AddError(validation, i => i.Exclusions.All(IsExclusionValid), "An exclusion is not of the form groupId:artifactId[:classifier[:extension]].");
    }

    /// <summary>
    /// Writes the coordinates where the original element has them: in its include, as <c>groupId:artifactId</c>, with
    /// the version when it is the same for every target framework and the include had one; else in metadata.
    /// </summary>
    protected override void Save(IkvmDependencyElementBuilder builder)
    {
        var inInclude = Original == null || MavenCoordinates.TryParseInclude(Original.Include, out _, out _, out _);
        if (inInclude)
        {
            var hadVersion = Original == null || (MavenCoordinates.TryParseInclude(Original.Include, out _, out _, out var version) && version != null);
            var versions = builder.Keys.Select(i => GetValues(i).Version).Distinct().ToList();
            var versionInInclude = hadVersion && versions.Count == 1 && versions[0].Length > 0;

            builder.Include = MavenCoordinates.Format(GroupId, ArtifactId, null, versionInInclude ? versions[0] : null);
            SetMetadata(builder, MavenReferenceRules.VersionMetadata, i => versionInInclude ? "" : i.Version);
        }
        else
        {
            SetMetadata(builder, MavenReferenceRules.VersionMetadata, i => i.Version);
        }

        SetMetadata(builder, MavenReferenceRules.ClassifierMetadata, i => i.Classifier);
        SetMetadata(builder, MavenReferenceRules.ScopeMetadata, i => i.Scope);
        SetMetadata(builder, MavenReferenceRules.OptionalMetadata, i => i.Optional ? "true" : "");
        SetMetadata(builder, MavenReferenceRules.ExclusionsMetadata, i => string.Join(";", i.Exclusions));
    }

}
