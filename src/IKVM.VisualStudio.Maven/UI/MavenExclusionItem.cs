namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// An exclusion in the list of a Maven entry.
/// </summary>
sealed class MavenExclusionItem
{

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public MavenExclusionItem(string exclusion, string? partialNote, bool isValid)
    {
        Exclusion = exclusion;
        PartialNote = partialNote;
        IsValid = isValid;
    }

    /// <summary>
    /// The exclusion as written, normally <c>groupId:artifactId[:classifier[:extension]]</c>.
    /// </summary>
    public string Exclusion { get; }

    /// <summary>
    /// The target frameworks the exclusion applies to, when that is only some of those being edited.
    /// </summary>
    public string? PartialNote { get; }

    /// <summary>
    /// Whether the exclusion applies to only some of the target frameworks being edited, which the list shows it
    /// differently for.
    /// </summary>
    public bool IsPartial => PartialNote != null;

    /// <summary>
    /// Whether the exclusion is of the form <c>groupId:artifactId[:classifier[:extension]]</c>; the list marks it
    /// when it is not.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// The tooltip of the exclusion in the list: the exclusion, with why it is not valid, or the target frameworks it
    /// applies to when that is only some.
    /// </summary>
    public string ToolTip => IsValid == false ? $"{Exclusion}\nNot of the form groupId:artifactId[:classifier[:extension]]." : PartialNote != null ? $"{Exclusion}\n{PartialNote}" : Exclusion;

}
