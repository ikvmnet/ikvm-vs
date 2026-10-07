using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Host.LanguageServer.Contracts;

using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;

using Nerdbank.Streams;

namespace IKVM.VisualStudio.LanguageServer;

/// <summary>
/// Connects Visual Studio to the Java language server, which runs in a ServiceHub process of its own: the connection
/// is the pipe of the language server's brokered service, which Visual Studio reads and writes directly.
/// </summary>
[Export(typeof(ILanguageClient))]
[ContentType(JavaContentTypeDefinition.Name)]
sealed class JavaLanguageClient : ILanguageClient
{

    Task? _serve;

    /// <summary>
    /// Raised to have Visual Studio start the server.
    /// </summary>
    public event AsyncEventHandler<EventArgs>? StartAsync;

    /// <summary>
    /// Raised to have Visual Studio stop the server, which it never is: the server stops with its connection.
    /// </summary>
    public event AsyncEventHandler<EventArgs>? StopAsync
    {
        add { }
        remove { }
    }

    /// <summary>
    /// Gets the name of the server, which Visual Studio shows.
    /// </summary>
    public string Name => "IKVM Java Language Server";

    /// <summary>
    /// Gets the settings sections sent to the server, of which there are none.
    /// </summary>
    public IEnumerable<string>? ConfigurationSections => null;

    /// <summary>
    /// Gets the options sent to the server when it initializes, of which there are none.
    /// </summary>
    public object? InitializationOptions => null;

    /// <summary>
    /// Gets the files whose changes are sent to the server, of which there are none.
    /// </summary>
    public IEnumerable<string>? FilesToWatch => null;

    /// <summary>
    /// Gets whether Visual Studio tells the user when the server fails to initialize.
    /// </summary>
    public bool ShowNotificationOnInitializeFailed => true;

    /// <summary>
    /// Starts the server once Visual Studio has loaded the client.
    /// </summary>
    public async Task OnLoadedAsync()
    {
        if (StartAsync != null)
            await StartAsync.InvokeAsync(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gives Visual Studio a connection to the Java language server: one end of a pipe, whose other end is passed to
    /// the language server's brokered service, which ServiceHub carries to its process.
    /// </summary>
    public Task<Connection?> ActivateAsync(CancellationToken token)
    {
        var (client, service) = FullDuplexStream.CreatePair();
        // served in the background, for as long as the connection lasts
        _serve = Task.Run(() => ServeAsync(service));
        return Task.FromResult<Connection?>(new Connection(client, client));
    }

    /// <summary>
    /// Requests the Java language server, and has it serve the Language Server Protocol over a pipe until the
    /// connection ends.
    /// </summary>
    static async Task ServeAsync(Stream stream)
    {
        try
        {
            var container = (IBrokeredServiceContainer?)await AsyncServiceProvider.GlobalProvider.GetServiceAsync(typeof(SVsBrokeredServiceContainer)) ?? throw new InvalidOperationException("No brokered service container.");
            var server = await container.GetFullAccessServiceBroker().GetProxyAsync<IJavaLanguageServer>(JavaLanguageServerDescriptors.Descriptor, JavaLanguageServerDescriptors.ActivationOptions, CancellationToken.None);
            using (server as IDisposable)
            {
                if (server == null)
                    throw new InvalidOperationException("The Java language server is not available.");

                await server.ServeAsync(stream.UsePipe(), CancellationToken.None);
            }
        }
        catch (Exception e)
        {
            ActivityLog.TryLogError(nameof(JavaLanguageClient), $"The Java language server failed: {e}");
        }
        finally
        {
            stream.Dispose();
        }
    }

    /// <summary>
    /// Called once the server has initialized.
    /// </summary>
    public Task OnServerInitializedAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Describes a failure of the server to initialize to the user.
    /// </summary>
    public Task<InitializationFailureContext?> OnServerInitializeFailedAsync(ILanguageClientInitializationInfo initializationState)
    {
        return Task.FromResult<InitializationFailureContext?>(new InitializationFailureContext()
        {
            FailureMessage = $"The IKVM Java language server failed to start: {initializationState.InitializationException?.Message}",
        });
    }

}
