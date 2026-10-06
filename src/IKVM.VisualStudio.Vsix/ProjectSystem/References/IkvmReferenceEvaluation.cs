using System.Collections.Generic;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// An <c>IkvmReference</c> item as MSBuild evaluates it for one target framework.
/// </summary>
/// <param name="Include">Evaluated include.</param>
/// <param name="Metadata">Evaluated metadata written on the item.</param>
internal sealed record IkvmReferenceEvaluation(string Include, IReadOnlyDictionary<string, string> Metadata);
