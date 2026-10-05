namespace IKVM.VisualStudio.ProjectSystem;

/// <summary>
/// Project capabilities relevant to the IKVM Dependencies node.
/// </summary>
public static class IkvmDependencyCapabilities
{

    /// <summary>
    /// Declared by the IKVM package for any project that can use <c>IkvmReference</c> items. The IKVM Dependencies
    /// node exists only in projects with this capability.
    /// </summary>
    public const string IkvmReferences = "IkvmReferences";

}
