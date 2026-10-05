using System.Collections.Generic;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// A metadata element written on an <c>IkvmReference</c> element, possibly limited to some target frameworks.
/// </summary>
/// <param name="Name">Metadata name.</param>
/// <param name="Value">Unevaluated value.</param>
/// <param name="TargetFrameworks">Target frameworks the value applies to, from its condition, or empty for all.</param>
internal sealed record IkvmReferenceMetadata(string Name, string Value, IReadOnlyList<string> TargetFrameworks);
