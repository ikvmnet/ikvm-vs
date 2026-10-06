namespace IKVM.VisualStudio.Maven;

/// <summary>
/// Maven coordinates as text.
/// </summary>
static class MavenCoordinates
{

    /// <summary>
    /// Formats coordinates as Maven writes them: <c>groupId:artifactId[:classifier][:version]</c>.
    /// </summary>
    public static string Format(string groupId, string artifactId, string? classifier, string? version)
    {
        var text = $"{groupId}:{artifactId}";
        if (string.IsNullOrEmpty(classifier) == false)
            text += ":" + classifier;
        if (string.IsNullOrEmpty(version) == false)
            text += ":" + version;

        return text;
    }

    /// <summary>
    /// Parses a <c>MavenReference</c> include of the form <c>groupId:artifactId[:version]</c>, as IKVM.Maven.Sdk does.
    /// </summary>
    public static bool TryParseInclude(string include, out string groupId, out string artifactId, out string? version)
    {
        groupId = artifactId = "";
        version = null;

        var parts = include.Split(':');
        if (parts.Length is not (2 or 3))
            return false;

        groupId = parts[0];
        artifactId = parts[1];
        version = parts.Length == 3 ? parts[2] : null;
        return true;
    }

}
