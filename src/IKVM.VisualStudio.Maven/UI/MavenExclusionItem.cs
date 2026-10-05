namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// An exclusion in the list of a Maven entry.
/// </summary>
sealed class MavenExclusionItem
{

    public MavenExclusionItem(string exclusion, string? partialNote, bool isValid)
    {
        Exclusion = exclusion;
        PartialNote = partialNote;
        IsValid = isValid;
    }

    public string Exclusion { get; }

    /// <summary>
    /// The target frameworks the exclusion applies to, when that is only some of those being edited.
    /// </summary>
    public string? PartialNote { get; }

    public bool IsPartial => PartialNote != null;

    public bool IsValid { get; }

    public string ToolTip => IsValid == false ? $"{Exclusion}\nNot of the form groupId:artifactId[:classifier[:extension]]." : PartialNote != null ? $"{Exclusion}\n{PartialNote}" : Exclusion;

}
