using System;

using Microsoft.ServiceHub.Framework;

namespace IKVM.VisualStudio.Host.Maven.Contracts;

/// <summary>
/// How the Maven service of the IKVM host is registered and requested.
/// </summary>
public static class MavenServiceDescriptors
{

    /// <summary>
    /// The name of the service, which its <c>.servicehub.service.json</c> file and pkgdef registration use too.
    /// </summary>
    public const string ServiceName = "IKVM.VisualStudio.Host.Maven";

    /// <summary>
    /// The moniker of version 1.0 of the service.
    /// </summary>
    public static ServiceMoniker Moniker { get; } = new ServiceMoniker(ServiceName, new Version(1, 0));

    /// <summary>
    /// The descriptor of the service: JSON-RPC over UTF-8 with HTTP-like headers.
    /// </summary>
    public static ServiceRpcDescriptor Descriptor { get; } = new ServiceJsonRpcDescriptor(Moniker, ServiceJsonRpcDescriptor.Formatters.UTF8, ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);

}
