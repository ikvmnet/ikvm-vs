using System;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Host.LanguageServer.Contracts;

using Microsoft.ServiceHub.Framework;
using Microsoft.ServiceHub.Framework.Services;

namespace IKVM.VisualStudio.Host.LanguageServer;

/// <summary>
/// Creates the Java language server for ServiceHub, which names this class in the service's
/// <c>.servicehub.service.json</c> file.
/// </summary>
public sealed class JavaLanguageServerFactory : IMultiVersionedServiceFactory
{

    /// <summary>
    /// Creates a new instance of the Java language server for a client.
    /// </summary>
    public Task<object> CreateAsync(IServiceProvider hostProvidedServices, ServiceMoniker serviceMoniker, ServiceActivationOptions serviceActivationOptions, IServiceBroker serviceBroker, AuthorizationServiceClient authorizationServiceClient, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<object>(new JavaLanguageServer());
    }

    /// <summary>
    /// Gets the descriptor of the Java language server, which is the same for every version requested.
    /// </summary>
    public ServiceRpcDescriptor GetServiceDescriptor(ServiceMoniker serviceMoniker)
    {
        return JavaLanguageServerDescriptors.Descriptor;
    }

}
