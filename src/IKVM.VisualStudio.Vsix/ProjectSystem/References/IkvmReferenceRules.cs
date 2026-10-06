namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// Names of the XAML rules and metadata shipped by the IKVM package for <c>IkvmReference</c> items.
/// </summary>
static class IkvmReferenceRules
{

    public const string ItemType = "IkvmReference";

    /// <summary>
    /// Rule over the evaluated <c>IkvmReference</c> items.
    /// </summary>
    public const string IkvmReference = "IkvmReference";

    /// <summary>
    /// Rule over the results of the <c>ResolveIkvmReferencesDesignTime</c> target.
    /// </summary>
    public const string ResolvedIkvmReference = "ResolvedIkvmReference";

    public const string IsResolvedMetadata = "IkvmIsResolved";
    public const string DiagnosticMetadata = "IkvmDiagnostic";
    public const string OriginalItemSpecMetadata = "OriginalItemSpec";
    public const string AssemblyNameMetadata = "AssemblyName";
    public const string AssemblyVersionMetadata = "AssemblyVersion";

}
