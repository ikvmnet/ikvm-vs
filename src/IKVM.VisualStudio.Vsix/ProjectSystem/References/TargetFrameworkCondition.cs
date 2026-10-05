using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Reads and writes item group conditions that select target frameworks, such as
/// <c>'$(TargetFramework)' == 'net8.0' Or '$(TargetFramework)' == 'net472'</c>.
/// </summary>
static class TargetFrameworkCondition
{

    static readonly Regex Clause = new Regex(@"^\s*'\$\(TargetFramework\)'\s*==\s*'([^'$@%]+)'\s*$", RegexOptions.Compiled);
    static readonly Regex Or = new Regex(@"\s+or\s+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    /// <summary>
    /// Parses a condition into the target frameworks it selects. An empty condition selects all target frameworks
    /// (an empty result). Returns <c>false</c> for conditions of any other form.
    /// </summary>
    public static bool TryParse(string? condition, out IReadOnlyList<string> targetFrameworks)
    {
        targetFrameworks = Array.Empty<string>();
        if (string.IsNullOrWhiteSpace(condition))
            return true;

        var names = new List<string>();
        foreach (var clause in Or.Split(condition!))
        {
            var match = Clause.Match(clause);
            if (match.Success == false)
                return false;

            names.Add(match.Groups[1].Value);
        }

        targetFrameworks = Normalize(names);
        return true;
    }

    /// <summary>
    /// Formats a condition selecting the given target frameworks, or an empty condition for all.
    /// </summary>
    public static string Format(IEnumerable<string> targetFrameworks)
    {
        var names = Normalize(targetFrameworks);
        return names.Count == 0 ? "" : " " + string.Join(" Or ", names.Select(i => $"'$(TargetFramework)' == '{i}'")) + " ";
    }

    /// <summary>
    /// Gets whether two sets of target frameworks are the same.
    /// </summary>
    public static bool AreSame(IEnumerable<string> a, IEnumerable<string> b)
    {
        return Normalize(a).SequenceEqual(Normalize(b), StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Removes duplicate target framework names and sorts the rest, ignoring case, so sets compare and format consistently.
    /// </summary>
    static IReadOnlyList<string> Normalize(IEnumerable<string> names)
    {
        return names.Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(i => i, StringComparer.OrdinalIgnoreCase).ToList();
    }

}
