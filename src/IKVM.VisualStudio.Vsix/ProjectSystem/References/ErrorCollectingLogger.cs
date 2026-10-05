using System.Collections.Generic;

using Microsoft.Build.Framework;

namespace IKVM.VisualStudio.Vsix.ProjectSystem.References;

/// <summary>
/// MSBuild logger that keeps the errors raised during a build.
/// </summary>
sealed class ErrorCollectingLogger : ILogger
{

    readonly List<string> _errors = new List<string>();

    public LoggerVerbosity Verbosity { get; set; } = LoggerVerbosity.Quiet;

    public string? Parameters { get; set; }

    /// <summary>
    /// Errors raised during the build.
    /// </summary>
    public IReadOnlyList<string> Errors
    {
        get
        {
            lock (_errors)
                return _errors.ToArray();
        }
    }

    public void Initialize(IEventSource eventSource)
    {
        eventSource.ErrorRaised += (sender, e) =>
        {
            lock (_errors)
                _errors.Add($"{e.Code}: {e.Message}");
        };
    }

    public void Shutdown()
    {

    }

}
