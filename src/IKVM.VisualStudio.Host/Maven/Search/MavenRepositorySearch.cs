using System;

using IKVM.VisualStudio.Host.Maven.Contracts;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// How a repository is searched, as probing it found.
/// </summary>
/// <param name="Method">How the repository is searched.</param>
/// <param name="Server">The search service of the repository, when it has one.</param>
/// <param name="Index">The index the repository publishes, when it has no search service.</param>
/// <param name="Message">What is searched, or why nothing is.</param>
/// <param name="Expires">When to probe the repository again.</param>
sealed record MavenRepositorySearch(MavenServiceSearchMethod Method, MavenSearchSource? Server, MavenIndex? Index, string Message, DateTime Expires);
