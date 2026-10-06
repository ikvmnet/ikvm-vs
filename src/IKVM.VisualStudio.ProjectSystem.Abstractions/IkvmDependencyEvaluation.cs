using System.Collections.Generic;

namespace IKVM.VisualStudio.ProjectSystem;

/// <summary>
/// An <c>IkvmReference</c> item as MSBuild evaluates it for one target framework.
/// </summary>
/// <param name="Include">Evaluated include.</param>
/// <param name="Metadata">Evaluated metadata written on the item.</param>
public sealed record IkvmDependencyEvaluation(string Include, IReadOnlyDictionary<string, string> Metadata);
