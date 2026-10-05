using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using IKVM.VisualStudio.Host.Maven.Contracts;

using org.eclipse.aether.util.version;
using org.eclipse.aether.version;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Collects artifacts found by searches, each once, with the newest version any of them found.
/// </summary>
sealed class MavenSearchResults
{

    static readonly VersionScheme VersionScheme = new GenericVersionScheme();
    static readonly Regex TimestampedSnapshot = new Regex(@"-\d{8}\.\d{6}-\d+$", RegexOptions.Compiled);

    readonly Dictionary<string, MavenServiceSearchResult> _byCoordinates = new(StringComparer.OrdinalIgnoreCase);
    readonly List<MavenServiceSearchResult> _results = new();

    public int Count => _results.Count;

    /// <summary>
    /// Adds an artifact, or a newer version of one already found; snapshots only when nothing else is known.
    /// </summary>
    public void Add(string groupId, string artifactId, string version)
    {
        if (string.IsNullOrEmpty(groupId) || string.IsNullOrEmpty(artifactId))
            return;

        var key = groupId + ":" + artifactId;
        if (_byCoordinates.TryGetValue(key, out var existing) == false)
        {
            _results.Add(_byCoordinates[key] = new MavenServiceSearchResult() { GroupId = groupId, ArtifactId = artifactId, LatestVersion = version ?? "" });
            return;
        }

        if (IsNewer(version, existing.LatestVersion))
            existing.LatestVersion = version!;
    }

    static bool IsNewer(string? version, string than)
    {
        if (string.IsNullOrEmpty(version))
            return false;
        if (string.IsNullOrEmpty(than))
            return true;

        var snapshot = IsSnapshot(version!);
        if (snapshot != IsSnapshot(than))
            return snapshot == false;

        try
        {
            return ((IComparable)VersionScheme.parseVersion(version)).CompareTo(VersionScheme.parseVersion(than)) > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Gets whether a version is a snapshot, as named or as deployed with a timestamp.
    /// </summary>
    static bool IsSnapshot(string version) => version.EndsWith("-SNAPSHOT", StringComparison.OrdinalIgnoreCase) || TimestampedSnapshot.IsMatch(version);

    /// <summary>
    /// Gets the artifacts found, those whose artifact ID matches the text best first, and otherwise in the order
    /// they were found.
    /// </summary>
    public IReadOnlyList<MavenServiceSearchResult> ToList(MavenSearchText text, int count)
    {
        var word = (text.ArtifactId ?? text.Text).Trim();
        return _results
            .Select((i, n) => (Result: i, Order: n))
            .OrderBy(i => Rank(i.Result, word))
            .ThenBy(i => i.Order)
            .Take(count)
            .Select(i => i.Result)
            .ToList();
    }

    static int Rank(MavenServiceSearchResult result, string word)
    {
        if (string.Equals(result.ArtifactId, word, StringComparison.OrdinalIgnoreCase))
            return 0;
        if (result.ArtifactId.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            return 1;

        return 2;
    }

}
