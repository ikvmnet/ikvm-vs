using System;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Host.Maven.Contracts;

using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;

namespace IKVM.VisualStudio.Host.Maven;

/// <summary>
/// Creates the Maven service for ServiceHub, which names this class in the service's
/// <c>.servicehub.service.json</c> file.
/// </summary>
public sealed class MavenServiceFactory : IMultiVersionedServiceFactory
{

    /// <summary>
    /// Creates a new instance of the Maven service for a client.
    /// </summary>
    public Task<object> CreateAsync(IServiceProvider hostProvidedServices, ServiceMoniker serviceMoniker, ServiceActivationOptions serviceActivationOptions, IServiceBroker serviceBroker, AuthorizationServiceClient authorizationServiceClient, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<object>(new MavenService());
    }

    /// <summary>
    /// Gets the descriptor of the Maven service, which is the same for every version requested.
    /// </summary>
    public ServiceRpcDescriptor GetServiceDescriptor(ServiceMoniker serviceMoniker)
    {
        return MavenServiceDescriptors.Descriptor;
    }

}
