using System;
using System.Collections.Generic;
using System.Linq;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// An <c>IkvmReference</c> item: written in the project file, or imported from another file.
/// </summary>
/// <param name="Include">Unevaluated include, or the evaluated include of one of the items an element with wildcards or lists produces.</param>
/// <param name="TargetFrameworks">Target frameworks the item applies to, or empty for all.</param>
/// <param name="Metadata">Metadata elements written on the element, in order.</param>
/// <param name="IsEditable">Whether the element can be edited and removed by the UI: it is in the project file, and its include, metadata and conditions are literal.</param>
/// <param name="DefinedIn">The file the element is imported from, or <c>null</c> for the project file.</param>
internal sealed record IkvmReferenceElement(string Include, IReadOnlyList<string> TargetFrameworks, IReadOnlyList<IkvmReferenceMetadata> Metadata, bool IsEditable, string? DefinedIn = null)
{

    /// <summary>
    /// The item as MSBuild evaluates it, by target framework, or under an empty key for a project with a single one.
    /// Empty for elements not yet evaluated.
    /// </summary>
    public IReadOnlyDictionary<string, IkvmReferenceEvaluation> Evaluations { get; init; } = new Dictionary<string, IkvmReferenceEvaluation>();

    /// <summary>
    /// Gets the unevaluated value of the named metadata for a target framework, or for all with an empty name: the
    /// last metadata element that applies, as MSBuild does.
    /// </summary>
    public string GetMetadata(string name, string targetFramework)
    {
        var result = "";
        foreach (var metadata in Metadata.Where(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)))
            if (metadata.TargetFrameworks.Count == 0 || metadata.TargetFrameworks.Contains(targetFramework, StringComparer.OrdinalIgnoreCase))
                result = metadata.Value;

        return result;
    }

}
