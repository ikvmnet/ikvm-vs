using System;
using System.Collections.Generic;
using System.Linq;

using IKVM.VisualStudio.Host.Maven.Contracts;

using java.io;
using java.util;

using org.apache.maven.repository.@internal;
using org.apache.maven.settings;
using org.apache.maven.settings.building;
using org.apache.maven.settings.crypto;
using org.eclipse.aether;
using org.eclipse.aether.connector.basic;
using org.eclipse.aether.impl;
using org.eclipse.aether.repository;
using org.eclipse.aether.spi.connector;
using org.eclipse.aether.spi.connector.transport;
using org.eclipse.aether.transport.file;
using org.eclipse.aether.transport.http;
using org.eclipse.aether.util.repository;
using org.sonatype.plexus.components.cipher;
using org.sonatype.plexus.components.sec.dispatcher;

using Path = System.IO.Path;

namespace IKVM.VisualStudio.Host.Maven;

/// <summary>
/// Maven Resolver with the user's Maven settings, as IKVM.Maven.Sdk sets it up for builds: <c>settings.xml</c> and
/// <c>settings-security.xml</c> in <c>~/.m2</c>, their mirrors, proxies, credentials and active profiles.
/// </summary>
sealed class MavenEnvironment
{

    sealed class SecDispatcher : DefaultSecDispatcher
    {

        public SecDispatcher(string configurationFile) :
            base(new DefaultPlexusCipher(), Collections.emptyMap(), configurationFile)
        {

        }

    }

    const string SettingsXml = "settings.xml";
    const string SettingsSecurityXml = "settings-security.xml";
    const string DefaultRepositoryType = "default";

    static readonly string UserHome = Path.Combine(java.lang.System.getProperty("user.home"), ".m2");

    /// <summary>
    /// How long to wait to connect, and for data: searches are interactive.
    /// </summary>
    const int Timeout = 15000;

    readonly Settings settings;

    public MavenEnvironment(IReadOnlyList<MavenServiceRepository> repositories)
    {
        settings = ReadSettings();

        var locator = CreateServiceLocator();
        RepositorySystem = (RepositorySystem)locator.getService(typeof(RepositorySystem));
        TransporterProvider = (TransporterProvider)locator.getService(typeof(TransporterProvider));
        Repositories = CreateRemoteRepositories(repositories);
    }

    public RepositorySystem RepositorySystem { get; }

    /// <summary>
    /// The transports of Maven Resolver, for requests other than resolving artifacts: searches, and indexes.
    /// </summary>
    public TransporterProvider TransporterProvider { get; }

    /// <summary>
    /// The repositories of the project, with those of the active profiles of the settings, and their policies and
    /// credentials from the settings.
    /// </summary>
    public List Repositories { get; }

    static IEnumerable<T> Iterate<T>(Collection? collection)
    {
        if (collection == null)
            yield break;

        var iterator = collection.iterator();
        while (iterator.hasNext())
            yield return (T)iterator.next();
    }

    static Settings ReadSettings()
    {
        var request = new DefaultSettingsBuildingRequest();
        request.setUserSettingsFile(new File(Path.Combine(UserHome, SettingsXml)));
        var settings = new DefaultSettingsBuilderFactory().newInstance().build(request).getEffectiveSettings();

        // decrypt passwords with settings-security.xml
        var securityFile = Path.Combine(UserHome, SettingsSecurityXml);
        if (System.IO.File.Exists(securityFile))
        {
            var decrypter = new DefaultSettingsDecrypter(new SecDispatcher(securityFile));
            var result = decrypter.decrypt(new DefaultSettingsDecryptionRequest(settings));
            settings.setServers(result.getServers());
            settings.setProxies(result.getProxies());
        }

        return settings;
    }

    static DefaultServiceLocator CreateServiceLocator()
    {
        var locator = MavenRepositorySystemUtils.newServiceLocator();
        locator.addService(typeof(RepositoryConnectorFactory), typeof(BasicRepositoryConnectorFactory));
        locator.addService(typeof(TransporterFactory), typeof(FileTransporterFactory));
        locator.addService(typeof(TransporterFactory), typeof(HttpTransporterFactory));
        return locator;
    }

    File GetLocalRepositoryDirectory()
    {
        return settings.getLocalRepository() is string path ? new File(path) : new File(Path.Combine(UserHome, "repository"));
    }

    /// <summary>
    /// Creates a session with the proxies, mirrors and credentials of the settings.
    /// </summary>
    public RepositorySystemSession CreateSession()
    {
        var session = MavenRepositorySystemUtils.newSession();
        session.setLocalRepositoryManager(RepositorySystem.newLocalRepositoryManager(session, new LocalRepository(GetLocalRepositoryDirectory())));
        session.setProxySelector(CreateProxySelector());
        session.setMirrorSelector(CreateMirrorSelector());
        session.setAuthenticationSelector(CreateAuthenticationSelector());
        session.setSystemProperty("java.version", java.lang.System.getProperty("java.version") ?? "1.8");
        session.setOffline(settings.isOffline());
        session.setConfigProperty(ConfigurationProperties.CONNECT_TIMEOUT, java.lang.Integer.valueOf(Timeout));
        session.setConfigProperty(ConfigurationProperties.REQUEST_TIMEOUT, java.lang.Integer.valueOf(Timeout));
        return session;
    }

    /// <summary>
    /// Gets the repositories as a session reaches them: the mirrors of the settings replace those they mirror, and
    /// each has its proxy and credentials.
    /// </summary>
    public IReadOnlyList<RemoteRepository> GetEffectiveRepositories(RepositorySystemSession session)
    {
        return Iterate<RemoteRepository>(RepositorySystem.newResolutionRepositories(session, Repositories)).ToList();
    }

    ProxySelector CreateProxySelector()
    {
        var selector = new DefaultProxySelector();
        foreach (var proxy in Iterate<org.apache.maven.settings.Proxy>(settings.getProxies()))
        {
            var builder = new AuthenticationBuilder();
            if (proxy.getUsername() is string username)
                builder.addUsername(username);
            if (proxy.getPassword() is string password)
                builder.addPassword(password);

            selector.add(new org.eclipse.aether.repository.Proxy(proxy.getProtocol(), proxy.getHost(), proxy.getPort(), builder.build()), proxy.getNonProxyHosts());
        }

        return selector;
    }

    MirrorSelector CreateMirrorSelector()
    {
        var selector = new DefaultMirrorSelector();
        foreach (var mirror in Iterate<Mirror>(settings.getMirrors()))
            selector.add(mirror.getId(), mirror.getUrl(), mirror.getLayout(), false, false, mirror.getMirrorOf(), mirror.getMirrorOfLayouts());

        return selector;
    }

    AuthenticationSelector CreateAuthenticationSelector()
    {
        var selector = new DefaultAuthenticationSelector();
        foreach (var server in Iterate<Server>(settings.getServers()))
            selector.add(server.getId(), CreateAuthentication(server));

        return new ConservativeAuthenticationSelector(selector);
    }

    static Authentication CreateAuthentication(Server server)
    {
        var builder = new AuthenticationBuilder();
        if (server.getUsername() is string username)
            builder.addUsername(username);
        if (server.getPassword() is string password)
            builder.addPassword(password);
        if (server.getPrivateKey() is string privateKey)
            builder.addPrivateKey(privateKey, server.getPassphrase());

        return builder.build();
    }

    List CreateRemoteRepositories(IReadOnlyList<MavenServiceRepository> import)
    {
        var map = new Dictionary<string, RemoteRepository.Builder>();
        var profiles = settings.getProfilesAsMap();
        var activeProfiles = Iterate<string>(settings.getActiveProfiles()).ToList();

        // the repositories of the active profiles, overridden by those of the project
        foreach (var profileId in activeProfiles)
            if (profiles.get(profileId) is Profile profile)
                foreach (var repository in Iterate<Repository>(profile.getRepositories()))
                    map[repository.getId()] = new RemoteRepository.Builder(repository.getId(), DefaultRepositoryType, repository.getUrl());

        foreach (var repository in import)
            map[repository.Id] = new RemoteRepository.Builder(repository.Id, DefaultRepositoryType, repository.Url);

        // policies from the active profiles
        foreach (var profileId in activeProfiles)
        {
            if (profiles.get(profileId) is not Profile profile)
                continue;

            foreach (var repository in Iterate<Repository>(profile.getRepositories()))
            {
                if (map.TryGetValue(repository.getId(), out var builder) == false)
                    continue;

                if (repository.getReleases() is { } releases)
                    builder.setPolicy(new org.eclipse.aether.repository.RepositoryPolicy(releases.isEnabled(), releases.getUpdatePolicy(), releases.getChecksumPolicy()));
                if (repository.getSnapshots() is { } snapshots)
                    builder.setSnapshotPolicy(new org.eclipse.aether.repository.RepositoryPolicy(snapshots.isEnabled(), snapshots.getUpdatePolicy(), snapshots.getChecksumPolicy()));
            }
        }

        // credentials from the servers of the settings
        foreach (var server in Iterate<Server>(settings.getServers()))
            if (map.TryGetValue(server.getId(), out var builder))
                builder.setAuthentication(CreateAuthentication(server));

        return Arrays.asList(map.Values.Select(i => (object)i.build()).ToArray());
    }

}
