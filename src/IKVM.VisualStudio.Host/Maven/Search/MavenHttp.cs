using System;

using org.eclipse.aether;
using org.eclipse.aether.repository;
using org.eclipse.aether.spi.connector.transport;

using java.net;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Requests other than resolving artifacts, to the servers of repositories, through the transports of Maven Resolver:
/// they go through the proxies of the settings, with the credentials of the repositories.
/// </summary>
sealed class MavenHttp
{

    readonly MavenEnvironment _maven;
    readonly RepositorySystemSession _session;

    public MavenHttp(MavenEnvironment maven, RepositorySystemSession session)
    {
        _maven = maven;
        _session = session;
    }

    public RepositorySystemSession Session => _session;

    /// <summary>
    /// Gets a repository at another URL of the server of a repository, such as the root of its API, with the
    /// credentials of that repository, and the proxy the settings choose for the URL.
    /// </summary>
    public RemoteRepository At(RemoteRepository repository, string url)
    {
        var builder = new RemoteRepository.Builder(repository.getId(), repository.getContentType(), url);
        builder.setAuthentication(repository.getAuthentication());
        builder.setProxy(_session.getProxySelector()?.getProxy(builder.build()) ?? repository.getProxy());
        return builder.build();
    }

    /// <summary>
    /// Gets a URL relative to a repository as text, or <c>null</c> when it does not exist.
    /// </summary>
    public string? GetString(RemoteRepository repository, string location)
    {
        return Invoke(repository, location, (transporter, task) =>
        {
            transporter.get(task);
            return task.getDataString();
        });
    }

    /// <summary>
    /// Downloads a URL relative to a repository to a file, failing with <see cref="java.io.FileNotFoundException"/>
    /// when it does not exist.
    /// </summary>
    public void Download(RemoteRepository repository, string location, java.io.File file, TransportListener? listener)
    {
        var found = Invoke(repository, location, (transporter, task) =>
        {
            task.setDataFile(file);
            if (listener != null)
                task.setListener(listener);

            transporter.get(task);
            return true;
        });

        if (found == false)
            throw new java.io.FileNotFoundException(location);
    }

    T? Invoke<T>(RemoteRepository repository, string location, Func<Transporter, GetTask, T> get)
    {
        var transporter = _maven.TransporterProvider.newTransporter(_session, repository);
        try
        {
            return get(transporter, new GetTask(URI.create(location)));
        }
        catch (Exception e) when (transporter.classify(e) == Transporter.ERROR_NOT_FOUND)
        {
            return default;
        }
        finally
        {
            transporter.close();
        }
    }

}
