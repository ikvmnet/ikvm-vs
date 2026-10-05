using System;

namespace IKVM.VisualStudio.Vsix.Commands;

/// <summary>
/// Command and menu IDs from <c>IkvmPackage.vsct</c>.
/// </summary>
static class IkvmDependenciesCommandIds
{

    public const string CommandSetString = "80eaf98f-5624-42e8-8c16-81fe7a56751e";

    public static readonly Guid CommandSet = new Guid(CommandSetString);

    public const int IkvmDependenciesRootMenu = 0x1000;
    public const int IkvmReferenceMenu = 0x1001;
    public const int ManageIkvmDependencies = 0x0100;

}
