using System;

using org.apache.maven.index.updater;
using org.eclipse.aether.repository;
using org.eclipse.aether.spi.connector.transport;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// Fetches the files of the index a repository publishes, in its <c>.index</c> directory, through the transports of
/// Maven Resolver: each is downloaded to a file, and then read, reporting how much has been downloaded.
/// </summary>
sealed class MavenIndexFetcher : ResourceFetcher
{

    /// <summary>
    /// Counts what is downloaded.
    /// </summary>
    sealed class Listener : TransportListener
    {

        readonly Action<long, long> _progress;
        long _length;
        long _received;

        public Listener(Action<long, long> progress)
        {
            _progress = progress;
        }

        public override void transportStarted(long dataOffset, long dataLength)
        {
            _received = dataOffset;
            _length = dataLength;
            _progress(_received, _length);
        }

        public override void transportProgressed(java.nio.ByteBuffer data)
        {
            _received += data.remaining();
            _progress(_received, _length);
        }

    }

    readonly MavenHttp _http;
    readonly RemoteRepository _repository;
    readonly java.io.File _directory;
    readonly Action<string, long, long> _progress;

    /// <param name="http">The transports.</param>
    /// <param name="repository">The repository whose index is fetched.</param>
    /// <param name="directory">Where files are downloaded to.</param>
    /// <param name="progress">Told the name of each file, and how much of it is downloaded, of how much.</param>
    public MavenIndexFetcher(MavenHttp http, RemoteRepository repository, java.io.File directory, Action<string, long, long> progress)
    {
        _http = http;
        _repository = repository;
        _directory = directory;
        _progress = progress;
    }

    public void connect(string id, string url)
    {
        _directory.mkdirs();
    }

    public void disconnect()
    {

    }

    public java.io.InputStream retrieve(string name)
    {
        var file = new java.io.File(_directory, name);
        file.delete();

        _http.Download(_repository, ".index/" + name, file, new Listener((received, length) => _progress(name, received, length)));
        return new DeletingInputStream(file);
    }

    /// <summary>
    /// Reads a downloaded file, and deletes it once read.
    /// </summary>
    sealed class DeletingInputStream : java.io.FileInputStream
    {

        readonly java.io.File _file;

        public DeletingInputStream(java.io.File file) :
            base(file)
        {
            _file = file;
        }

        public override void close()
        {
            base.close();
            _file.delete();
        }

    }

}
