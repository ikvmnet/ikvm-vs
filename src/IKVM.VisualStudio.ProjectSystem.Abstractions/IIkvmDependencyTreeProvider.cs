using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.ProjectSystem;

/// <summary>
/// Adds nodes under the IKVM Dependencies node: directly under it in projects with a single target framework, else
/// under the folder of each target framework.
/// </summary>
/// <remarks>
/// Export implementations with <c>[Export(typeof(IIkvmDependencyTreeProvider))]</c> and an <c>[AppliesTo]</c>
/// naming the capability of the projects they apply to. Implementations hold no state: the IKVM extension subscribes
/// to the rules of each configured project and calls <see cref="UpdateTreeAsync"/> with the latest snapshots whenever
/// the tree changes.
/// </remarks>
[ProjectSystemContract(ProjectSystemContractScope.UnconfiguredProject, ProjectSystemContractProvider.Extension)]
public interface IIkvmDependencyTreeProvider
{

    /// <summary>
    /// Gets the names of the rules the provider reads, from <c>ProjectSubscriptionService</c> contexts.
    /// </summary>
    IReadOnlyCollection<string> RuleNames { get; }

    /// <summary>
    /// Updates the provider's nodes under the given parent, for one configured project. The provider adds, updates
    /// and removes only nodes it added itself, recognizing them by its own flags, and returns the updated parent.
    /// </summary>
    Task<IProjectTree> UpdateTreeAsync(IIkvmDependencyTreeContext context, IProjectTree parent, CancellationToken cancellationToken);

}
