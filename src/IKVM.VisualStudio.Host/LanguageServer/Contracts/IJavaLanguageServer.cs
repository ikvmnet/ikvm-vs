using System.IO.Pipelines;
using System.Threading;
using System.Threading.Tasks;

namespace IKVM.VisualStudio.Host.LanguageServer.Contracts;

/// <summary>
/// The Java language server, in the IKVM host.
/// </summary>
public interface IJavaLanguageServer
{

    /// <summary>
    /// Serves the Language Server Protocol over a pipe, completing once the client closes it.
    /// </summary>
    Task ServeAsync(IDuplexPipe pipe, CancellationToken cancellationToken);

}
