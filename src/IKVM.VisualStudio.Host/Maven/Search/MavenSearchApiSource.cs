using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using IKVM.VisualStudio.Host.Maven.Contracts;

using org.apache.maven.search;
using org.apache.maven.search.request;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Searches through a backend of the Maven Indexer search API: the search service of Maven Central, or an index.
/// </summary>
sealed class MavenSearchApiSource : MavenSearchSource
{

    readonly SearchBackend _backend;
    readonly int _recordsPerArtifact;

    /// <summary>
    /// Searches through a backend of the search API, shown by a name.
    /// </summary>
    /// <param name="name">What searches the repository, as it is shown.</param>
    /// <param name="backend">The backend.</param>
    /// <param name="recordsPerArtifact">How many records to ask for for each artifact wanted: an index has a record
    /// for each version of an artifact.</param>
    public MavenSearchApiSource(string name, SearchBackend backend, int recordsPerArtifact = 1)
    {
        Name = name;
        _backend = backend;
        _recordsPerArtifact = recordsPerArtifact;
    }

    /// <inheritdoc />
    public override string Name { get; }

    /// <summary>
    /// Searches the backend for enough records to find the artifacts wanted, and collects the artifact of each.
    /// </summary>
    public override IReadOnlyList<MavenServiceSearchResult> Search(MavenSearchText text, int count)
    {
        var response = _backend.search(new SearchRequest(new Paging(Math.Max(1, count * _recordsPerArtifact)), ToQuery(text)));

        var results = new MavenSearchResults();
        foreach (var record in ((IEnumerable)response.getPage()).Cast<Record>())
            results.Add(record.getValue(MAVEN.GROUP_ID) ?? "", record.getValue(MAVEN.ARTIFACT_ID) ?? "", record.getValue(MAVEN.VERSION) ?? "");

        return results.ToList(text, count);
    }

    /// <summary>
    /// Coordinates search their own fields; anything else searches the text.
    /// </summary>
    static Query ToQuery(MavenSearchText text)
    {
        if (text.IsCoordinates)
            return BooleanQuery.and(FieldQuery.fieldQuery(MAVEN.GROUP_ID, text.GroupId), FieldQuery.fieldQuery(MAVEN.ARTIFACT_ID, text.ArtifactId));

        return Query.query(text.Text);
    }

}
