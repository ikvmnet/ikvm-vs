using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.ProjectSystem.UI;

/// <summary>
/// Supplies the entries of one item type to the Manage IKVM Dependencies dialog.
/// </summary>
/// <remarks>
/// Export implementations with <c>[Export(typeof(IkvmDependencyEntryProvider))]</c> and an <c>[AppliesTo]</c> naming
/// the capability of the projects they apply to.
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension)]
public abstract class IkvmDependencyEntryProvider
{

    /// <summary>
    /// Gets the item type the entries of the provider stand for.
    /// </summary>
    public abstract string ItemType { get; }

    /// <summary>
    /// Creates the entry for an item already in the project.
    /// </summary>
    public abstract IkvmDependencyEntry CreateEntry(IkvmDependencyEntryContext context, IkvmDependencyElement element);

    /// <summary>
    /// Gets the commands that add entries, shown above the list.
    /// </summary>
    public virtual IReadOnlyList<IkvmDependencyAddCommand> GetAddCommands(IkvmDependencyEntryContext context) => Array.Empty<IkvmDependencyAddCommand>();

    /// <summary>
    /// Creates entries for files or directories dropped on the list, for those the provider takes; none by default.
    /// Paths already listed are skipped.
    /// </summary>
    public virtual IReadOnlyList<IkvmDependencyEntry> CreateEntries(IkvmDependencyEntryContext context, IReadOnlyList<string> paths) => Array.Empty<IkvmDependencyEntry>();

    /// <summary>
    /// Loads what the entries of the provider show that takes time to find, once they are listed: for the entries read
    /// from the project when the dialog opens, then for those added.
    /// </summary>
    public virtual Task LoadAsync(IkvmDependencyEntryContext context, IReadOnlyList<IkvmDependencyEntry> entries, CancellationToken cancellationToken) => Task.CompletedTask;

}
