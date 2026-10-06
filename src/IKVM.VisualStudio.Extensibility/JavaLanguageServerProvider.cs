using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Host.LanguageServer.Contracts;

using Microsoft.VisualStudio.Extensibility;
using Microsoft.VisualStudio.Extensibility.Editor;
using Microsoft.VisualStudio.Extensibility.LanguageServer;
using Microsoft.VisualStudio.RpcContracts.LanguageServerProvider;

using Nerdbank.Streams;

namespace IKVM.VisualStudio.Extensibility;

/// <summary>
/// Provides the Java language server for <c>.java</c> files: the server runs in the IKVM host, and this connects Visual
/// Studio to it.
/// </summary>
[VisualStudioContribution]
internal sealed class JavaLanguageServerProvider : LanguageServerProvider
{

    readonly TraceSource _trace;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public JavaLanguageServerProvider(ExtensionCore container, VisualStudioExtensibility extensibilityObject, TraceSource trace) :
        base(container, extensibilityObject)
    {
        _trace = trace;
    }

    /// <inheritdoc />
    public override LanguageServerProviderConfiguration LanguageServerProviderConfiguration => new(
        "IKVM Java Language Server",
        [DocumentFilter.FromDocumentType(JavaDocumentType.Configuration)]);

    /// <summary>
    /// Starts the Java language server in the IKVM host, on one end of a pipe, and gives Visual Studio the other.
    /// </summary>
    public override Task<IDuplexPipe?> CreateServerConnectionAsync(CancellationToken cancellationToken)
    {
        var (toVisualStudio, toServer) = FullDuplexStream.CreatePair();
        _ = ServeAsync(toServer);
        return Task.FromResult<IDuplexPipe?>(new DuplexPipe(toVisualStudio.UsePipeReader(), toVisualStudio.UsePipeWriter()));
    }

    /// <summary>
    /// Connects the server's end of the pipe to the Java language server, in a process of its own, until Visual Studio
    /// closes it.
    /// </summary>
    async Task ServeAsync(Stream stream)
    {
        try
        {
            var server = await Extensibility.ServiceBroker.GetProxyAsync<IJavaLanguageServer>(JavaLanguageServerDescriptors.Descriptor, JavaLanguageServerDescriptors.ActivationOptions, CancellationToken.None);
            using (server as IDisposable)
            {
                if (server == null)
                    throw new InvalidOperationException("The Java language server of the IKVM host is not available.");

                await server.ServeAsync(stream.UsePipe(), CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            _trace.TraceEvent(TraceEventType.Error, 0, "The Java language server failed: {0}", e);
        }
        finally
        {
            await stream.DisposeAsync();
        }
    }

    /// <summary>
    /// Stops trying to start the server once it has failed.
    /// </summary>
    public override Task OnServerInitializationResultAsync(ServerInitializationResult serverInitializationResult, LanguageServerInitializationFailureInfo? initializationFailureInfo, CancellationToken cancellationToken)
    {
        if (serverInitializationResult == ServerInitializationResult.Failed)
            Enabled = false;

        return base.OnServerInitializationResultAsync(serverInitializationResult, initializationFailureInfo, cancellationToken);
    }

}
