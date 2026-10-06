using System;
using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Host.LanguageServer.Contracts;

using Nerdbank.Streams;

namespace IKVM.VisualStudio.Host.LanguageServer;

/// <summary>
/// Serves the Language Server Protocol for Java with Eclipse JDT LS, in a host process of its own, which ends once no
/// connection remains: Eclipse cannot start again in a process, and its threads would keep the process from exiting.
/// </summary>
sealed class JavaLanguageServer : IJavaLanguageServer
{

    /// <summary>
    /// How long the process waits, after its last connection ends, before it exits: long enough for the end of the
    /// connection to reach the client.
    /// </summary>
    static readonly TimeSpan ExitDelay = TimeSpan.FromSeconds(2);

    static int _connections;

    /// <inheritdoc />
    public Task ServeAsync(IDuplexPipe pipe, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _connections);

        // JDT LS reads and writes its streams synchronously, for as long as the client stays connected
        var serve = Task.Factory.StartNew(() =>
        {
            using var stream = pipe.AsStream();
            using var registration = cancellationToken.Register(stream.Dispose);
            ikvm.visualstudio.jdtls.JavaLanguageServer.serve(stream);
        }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        _ = serve.ContinueWith(_ => ExitWhenUnusedAsync(), TaskScheduler.Default).Unwrap();
        return serve;
    }

    /// <summary>
    /// Ends the process once the last connection has ended, unless another arrives meanwhile.
    /// </summary>
    static async Task ExitWhenUnusedAsync()
    {
        if (Interlocked.Decrement(ref _connections) > 0)
            return;

        await Task.Delay(ExitDelay);
        if (Volatile.Read(ref _connections) == 0)
            Environment.Exit(0);
    }

}
