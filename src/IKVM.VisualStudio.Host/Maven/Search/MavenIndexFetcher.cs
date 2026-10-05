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

        /// <summary>
        /// Initializes a new instance that tells <paramref name="progress"/> how much is downloaded, of how much.
        /// </summary>
        public Listener(Action<long, long> progress)
        {
            _progress = progress;
        }

        /// <summary>
        /// Starts counting from where the download resumes, of the length of the data.
        /// </summary>
        public override void transportStarted(long dataOffset, long dataLength)
        {
            _received = dataOffset;
            _length = dataLength;
            _progress(_received, _length);
        }

        /// <summary>
        /// Counts the data received.
        /// </summary>
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

    /// <summary>
    /// Fetches the index of a repository into a directory, telling how much of each file is downloaded.
    /// </summary>
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

    /// <summary>
    /// Creates the directory files are downloaded to; the repository is already known.
    /// </summary>
    public void connect(string id, string url)
    {
        _directory.mkdirs();
    }

    /// <summary>
    /// Does nothing: each request uses a transporter of its own, which it closes.
    /// </summary>
    public void disconnect()
    {

    }

    /// <summary>
    /// Downloads a file of the <c>.index</c> directory of the repository to a file, reporting progress, and returns
    /// a stream that reads it, and deletes it once closed.
    /// </summary>
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

        /// <summary>
        /// Initializes a new instance that reads, and then deletes, the given file.
        /// </summary>
        public DeletingInputStream(java.io.File file) :
            base(file)
        {
            _file = file;
        }

        /// <summary>
        /// Closes the stream, and deletes the file.
        /// </summary>
        public override void close()
        {
            base.close();
            _file.delete();
        }

    }

}
