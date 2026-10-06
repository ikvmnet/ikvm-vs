using System.Collections.Generic;
using System.Linq;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// Collects what is wrong with an entry.
/// </summary>
public sealed class IkvmDependencyValidation
{

    readonly bool _hasTargetFrameworks;
    readonly List<string> _errors = new List<string>();

    internal IkvmDependencyValidation(bool hasTargetFrameworks)
    {
        _hasTargetFrameworks = hasTargetFrameworks;
    }

    internal IReadOnlyList<string> Errors => _errors;

    /// <summary>
    /// Adds an error.
    /// </summary>
    public void AddError(string message)
    {
        _errors.Add(message);
    }

    /// <summary>
    /// Adds an error for the given target framework keys, naming them when the project has several. Nothing is added
    /// for no keys.
    /// </summary>
    public void AddError(IEnumerable<string> keys, string message)
    {
        var list = keys.Distinct().ToList();
        if (list.Count == 0)
            return;

        _errors.Add(_hasTargetFrameworks ? $"{message} ({string.Join(", ", list)})" : message);
    }

}
