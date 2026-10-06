using System.Collections.Generic;

namespace IKVM.VisualStudio.ProjectSystem;

/// <summary>
/// A metadata element written on an <c>IkvmReference</c> element, possibly limited to some target frameworks.
/// </summary>
/// <param name="Name">Metadata name.</param>
/// <param name="Value">Unevaluated value.</param>
/// <param name="TargetFrameworks">Target frameworks the value applies to, from its condition, or empty for all.</param>
public sealed record IkvmDependencyMetadata(string Name, string Value, IReadOnlyList<string> TargetFrameworks);
