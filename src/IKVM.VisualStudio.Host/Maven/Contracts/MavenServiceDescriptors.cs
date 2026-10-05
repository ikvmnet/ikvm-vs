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

    public static ServiceMoniker Moniker { get; } = new ServiceMoniker(ServiceName, new Version(1, 0));

    public static ServiceRpcDescriptor Descriptor { get; } = new ServiceJsonRpcDescriptor(Moniker, ServiceJsonRpcDescriptor.Formatters.UTF8, ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders);

}
