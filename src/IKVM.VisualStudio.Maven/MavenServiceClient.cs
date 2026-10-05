using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Host.Contracts;
using IKVM.VisualStudio.Host.Maven.Contracts;

using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;

namespace IKVM.VisualStudio.Maven;

/// <summary>
/// Calls the Maven service of the IKVM host, the process ServiceHub runs the services that need IKVM in. Each call
/// passes the repositories of the project.
/// </summary>
static class MavenServiceClient
{

    /// <summary>
    /// Gets the versions of an artifact in the repositories of a project, newest first, or none when the service
    /// cannot list them.
    /// </summary>
    public static Task<IReadOnlyList<string>> GetVersionsAsync(ConfiguredProject project, string groupId, string artifactId, CancellationToken cancellationToken)
    {
        return InvokeAsync<IReadOnlyList<string>>(project, async (service, repositories) => await service.GetVersionsAsync(repositories, groupId, artifactId, cancellationToken), Array.Empty<string>(), $"list the versions of {groupId}:{artifactId}", cancellationToken);
    }

    /// <summary>
    /// Gets how each repository of a project is searched, or none when the service cannot tell. Repositories searched
    /// through the indexes they publish start downloading or updating them.
    /// </summary>
    public static Task<IReadOnlyList<MavenServiceSearchStatus>> GetSearchStatusAsync(ConfiguredProject project, CancellationToken cancellationToken)
    {
        return InvokeAsync<IReadOnlyList<MavenServiceSearchStatus>>(project, async (service, repositories) => await service.GetSearchStatusAsync(repositories, cancellationToken), Array.Empty<MavenServiceSearchStatus>(), "check the Maven repositories for search", cancellationToken);
    }

    /// <summary>
    /// Searches the repositories of a project that can be searched for artifacts matching some text, telling
    /// <paramref name="updated"/> each time the search of a repository completes or fails, with everything found so
    /// far.
    /// </summary>
    public static Task SearchAsync(ConfiguredProject project, string text, int count, Action<MavenServiceSearchUpdate, IReadOnlyList<MavenSearchResult>> updated, CancellationToken cancellationToken)
    {
        return InvokeAsync(project, async (service, repositories) =>
        {
            await foreach (var update in service.SearchAsync(repositories, text, count, cancellationToken))
                updated(update, update.Results.Select(i => new MavenSearchResult(i.GroupId, i.ArtifactId, i.LatestVersion)).ToList());

            return true;
        }, false, $"search for '{text}'", cancellationToken, rethrow: true);
    }

    /// <summary>
    /// Calls the service with the repositories of a project, returning the fallback, and logging why, when that fails;
    /// or failing, with <paramref name="rethrow"/>, so that the caller can show why.
    /// </summary>
    static async Task<T> InvokeAsync<T>(ConfiguredProject project, Func<IMavenService, MavenServiceRepository[], Task<T>> call, T fallback, string what, CancellationToken cancellationToken, bool rethrow = false)
    {
        try
        {
            var repositories = (await MavenProjectQueries.GetRepositoriesAsync(project)).Select(i => new MavenServiceRepository() { Id = i.Id, Url = i.Url }).ToArray();

            var container = (IBrokeredServiceContainer?)await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsBrokeredServiceContainer)) ?? throw new InvalidOperationException("No brokered service container.");
            var service = await container.GetFullAccessServiceBroker().GetProxyAsync<IMavenService>(MavenServiceDescriptors.Descriptor, IkvmHost.ActivationOptions, cancellationToken);
            using (service as IDisposable)
            {
                if (service == null)
                    throw new InvalidOperationException("The Maven service is not available.");

                return await call(service, repositories);
            }
        }
        catch (Exception e) when (cancellationToken.IsCancellationRequested == false)
        {
            ActivityLog.TryLogWarning(nameof(MavenServiceClient), $"Could not {what}: {e}");
            if (rethrow)
                throw;

            return fallback;
        }
    }

}
