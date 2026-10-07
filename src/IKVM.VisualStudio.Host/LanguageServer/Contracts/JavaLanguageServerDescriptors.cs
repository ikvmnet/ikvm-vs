using System;

using IKVM.VisualStudio.Host.Contracts;

using Microsoft.ServiceHub.Framework;

using Nerdbank.Streams;

namespace IKVM.VisualStudio.Host.LanguageServer.Contracts;

/// <summary>
/// How the Java language server of the IKVM host is registered and requested.
/// </summary>
public static class JavaLanguageServerDescriptors
{

    /// <summary>
    /// The name of the service, which its <c>.servicehub.service.json</c> file and pkgdef registration use too.
    /// </summary>
    public const string ServiceName = "IKVM.VisualStudio.Host.JavaLanguageServer";

    /// <summary>
    /// The host group of the service: a process of its own, as Eclipse expects to own the process it runs in, and
    /// cannot start again in it once stopped. The process ends with the connection it serves.
    /// </summary>
    public const string HostGroup = "IKVM.JavaLanguageServer";

    /// <summary>
    /// The options that request the service in a process of its own.
    /// </summary>
    public static ServiceActivationOptions ActivationOptions => IkvmHost.GetActivationOptions(HostGroup);

    /// <summary>
    /// The moniker of the service.
    /// </summary>
    public static ServiceMoniker Moniker { get; } = new ServiceMoniker(ServiceName, new Version(1, 0));

    /// <summary>
    /// The descriptor of the service: JSON-RPC over a multiplexing stream, which carries the pipe the Language Server
    /// Protocol is served over.
    /// </summary>
    public static ServiceRpcDescriptor Descriptor { get; } = new ServiceJsonRpcDescriptor(
        Moniker,
        clientInterface: null,
        ServiceJsonRpcDescriptor.Formatters.UTF8,
        ServiceJsonRpcDescriptor.MessageDelimiters.HttpLikeHeaders,
        new MultiplexingStream.Options() { ProtocolMajorVersion = 3 });

}
