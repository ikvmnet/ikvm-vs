using org.apache.maven.search;
using org.apache.maven.search.backend.smo.@internal;
using org.eclipse.aether.repository;

using java.net;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Carries the requests of the backend of the search API for the search service of Maven Central through the
/// transports of Maven Resolver, so that they go through the proxies of the settings.
/// </summary>
sealed class MavenSmoTransport : SmoSearchTransportSupport
{

    readonly MavenHttp _http;
    readonly RemoteRepository _repository;

    /// <param name="http">The transports.</param>
    /// <param name="repository">The repository the service searches, whose credentials are used.</param>
    public MavenSmoTransport(MavenHttp http, RemoteRepository repository)
    {
        _http = http;
        _repository = repository;
    }

    public override string fetch(SearchRequest searchRequest, string serviceUri)
    {
        var uri = URI.create(serviceUri);
        var root = _http.At(_repository, uri.getScheme() + "://" + uri.getRawAuthority() + "/");
        var location = uri.getRawPath().TrimStart('/') + (uri.getRawQuery() is string query ? "?" + query : "");
        return _http.GetString(root, location) ?? throw new java.io.FileNotFoundException(serviceUri);
    }

}
