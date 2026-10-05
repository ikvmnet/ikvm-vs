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

    public Task<object> CreateAsync(IServiceProvider hostProvidedServices, ServiceMoniker serviceMoniker, ServiceActivationOptions serviceActivationOptions, IServiceBroker serviceBroker, AuthorizationServiceClient authorizationServiceClient, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<object>(new MavenService());
    }

    public ServiceRpcDescriptor GetServiceDescriptor(ServiceMoniker serviceMoniker)
    {
        return MavenServiceDescriptors.Descriptor;
    }

}
