namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Text to search for: <c>groupId:artifactId</c>, which searches those fields, or else words.
/// </summary>
sealed record MavenSearchText(string Text, string? GroupId, string? ArtifactId)
{

    public static MavenSearchText Parse(string text)
    {
        text = text.Trim();

        var parts = text.Split(':');
        if (parts.Length >= 2 && parts[0].Length > 0 && parts[1].Length > 0)
            return new MavenSearchText(text, parts[0], parts[1]);

        return new MavenSearchText(text, null, null);
    }

    public bool IsCoordinates => GroupId != null && ArtifactId != null;

}
