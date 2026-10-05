using System;

namespace IKVM.VisualStudio.ProjectSystem;

/// <summary>
/// The command set, menus and groups of the IKVM Dependencies tree. Other extensions place their own commands in
/// these groups from their <c>.vsct</c> files, and handle them with their own command group handlers.
/// </summary>
public static class IkvmDependencyCommandIds
{

    public const string CommandSetString = "80eaf98f-5624-42e8-8c16-81fe7a56751e";

    /// <summary>
    /// Gets the command set of the menus, groups and commands.
    /// </summary>
    public static readonly Guid CommandSet = new Guid(CommandSetString);

    /// <summary>
    /// The context menu of the IKVM Dependencies node and of its target framework folders.
    /// </summary>
    public const int IkvmDependenciesRootMenu = 0x1000;

    /// <summary>
    /// The context menu of nodes flagged <see cref="IkvmDependencyTreeFlags.Reference"/>.
    /// </summary>
    public const int IkvmReferenceMenu = 0x1001;

    /// <summary>
    /// The context menu of nodes flagged <see cref="IkvmDependencyTreeFlags.JarFile"/>.
    /// </summary>
    public const int IkvmJarFileMenu = 0x1002;

    /// <summary>
    /// The group of <see cref="IkvmDependenciesRootMenu"/> holding Manage IKVM Dependencies.
    /// </summary>
    public const int IkvmDependenciesRootGroup = 0x1100;

    /// <summary>
    /// The group of <see cref="IkvmReferenceMenu"/> holding Remove.
    /// </summary>
    public const int IkvmReferenceGroup = 0x1101;

    /// <summary>
    /// The last group of <see cref="IkvmReferenceMenu"/>, holding Properties.
    /// </summary>
    public const int IkvmReferencePropertiesGroup = 0x1102;

    /// <summary>
    /// The first group of <see cref="IkvmJarFileMenu"/>, holding Open Containing Folder.
    /// </summary>
    public const int IkvmJarFileGroup = 0x1104;

    /// <summary>
    /// The last group of <see cref="IkvmJarFileMenu"/>, holding Properties.
    /// </summary>
    public const int IkvmJarFilePropertiesGroup = 0x1105;

    /// <summary>
    /// The group of <see cref="IkvmDependenciesRootMenu"/> holding the add commands of the entry providers.
    /// </summary>
    public const int IkvmDependenciesAddGroup = 0x1106;

    /// <summary>
    /// The Manage IKVM Dependencies command.
    /// </summary>
    public const int ManageIkvmDependencies = 0x0100;

    /// <summary>
    /// The Open Containing Folder command of a JAR file.
    /// </summary>
    public const int OpenContainingFolder = 0x0101;

    /// <summary>
    /// The first of the add commands of the entry providers, one after another.
    /// </summary>
    public const int AddDependencyFirst = 0x0200;

    /// <summary>
    /// The last of the add commands of the entry providers.
    /// </summary>
    public const int AddDependencyLast = 0x02FF;

}
