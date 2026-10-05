using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

using org.apache.maven.index;
using org.apache.maven.index.context;
using org.apache.maven.index.creator;
using org.apache.maven.index.incremental;
using org.apache.maven.index.updater;
using org.apache.maven.search.backend.indexer.@internal;
using org.eclipse.aether.repository;

using java.util;

namespace IKVM.VisualStudio.Host.Maven.Search;

/// <summary>
/// The index a repository publishes, in Maven Indexer's format, downloaded to
/// <c>%LOCALAPPDATA%\IKVM\MavenIndex</c>, and updated in the background, incrementally where the repository allows it.
/// There is one for each repository in the process, which keeps it open, and updates it, for as long as it runs.
/// </summary>
sealed class MavenIndex
{

    /// <summary>
    /// How often to check a repository for a newer index.
    /// </summary>
    static readonly TimeSpan UpdateInterval = TimeSpan.FromHours(24);

    /// <summary>
    /// How long to wait after an update fails before trying again.
    /// </summary>
    static readonly TimeSpan RetryInterval = TimeSpan.FromHours(1);

    /// <summary>
    /// How many records to read for each artifact wanted: the index has one for each version.
    /// </summary>
    const int RecordsPerArtifact = 10;

    static readonly string Root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "IKVM", "MavenIndex");
    static readonly ConcurrentDictionary<string, MavenIndex> Indexes = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the index of a repository.
    /// </summary>
    public static MavenIndex Get(RemoteRepository repository)
    {
        var url = repository.getUrl().TrimEnd('/') + "/";
        return Indexes.GetOrAdd(url, _ => new MavenIndex(repository.getId(), url));
    }

    readonly object _lock = new();
    readonly string _repositoryId;
    readonly string _url;
    readonly string _directory;
    Indexer? _indexer;
    IndexingContext? _context;
    Task? _update;
    DateTime _failed = DateTime.MinValue;
    string _message = "";

    MavenIndex(string repositoryId, string url)
    {
        _repositoryId = repositoryId;
        _url = url;
        _directory = Path.Combine(Root, GetDirectoryName(repositoryId, url));
    }

    /// <summary>
    /// Gets a name for the directory of the index of a repository: its ID, and a hash of its URL.
    /// </summary>
    static string GetDirectoryName(string repositoryId, string url)
    {
        var id = new string(repositoryId.Select(i => char.IsLetterOrDigit(i) || i is '.' or '-' or '_' ? i : '_').ToArray());
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(url.ToLowerInvariant()));
        return id + "-" + string.Concat(hash.Take(6).Select(i => i.ToString("x2")));
    }

    string CheckedFile => Path.Combine(_directory, "checked");

    /// <summary>
    /// Whether the index has been downloaded, and so can be searched.
    /// </summary>
    public bool IsReady
    {
        get
        {
            lock (_lock)
                return _context?.getTimestamp() != null;
        }
    }

    /// <summary>
    /// Whether the index is being downloaded or updated.
    /// </summary>
    public bool IsUpdating
    {
        get
        {
            lock (_lock)
                return _update is { IsCompleted: false };
        }
    }

    /// <summary>
    /// What is happening to the index, or how new it is.
    /// </summary>
    public string Message
    {
        get
        {
            lock (_lock)
            {
                if (_update is { IsCompleted: false } || _message.Length > 0)
                    return _message;

                return _context?.getTimestamp() is Date timestamp ? $"Index of {DateTimeOffset.FromUnixTimeMilliseconds(timestamp.getTime()).LocalDateTime:d}." : "Index not downloaded.";
            }
        }
    }

    /// <summary>
    /// Gets a source that searches the index, once it has been downloaded.
    /// </summary>
    public MavenSearchSource? GetSource()
    {
        lock (_lock)
        {
            Open();
            if (_indexer == null || _context?.getTimestamp() == null)
                return null;

            return new MavenSearchApiSource("Index", new IndexerCoreSearchBackendImpl(_indexer, _context), RecordsPerArtifact);
        }
    }

    /// <summary>
    /// Starts downloading or updating the index in the background, unless it is already, or was checked recently.
    /// </summary>
    public void Update(MavenHttp http, RemoteRepository repository)
    {
        lock (_lock)
        {
            if (_update is { IsCompleted: false })
                return;

            var now = DateTime.UtcNow;
            if (now - _failed < RetryInterval)
                return;

            Open();
            if (_context == null)
                return;

            if (_context.getTimestamp() != null && File.Exists(CheckedFile) && now - File.GetLastWriteTimeUtc(CheckedFile) < UpdateInterval)
                return;

            _message = "Checking for a newer index...";
            _update = Task.Run(() => Download(http, repository));
        }
    }

    /// <summary>
    /// Opens the index on disk, unless it is open, or another process has it open.
    /// </summary>
    void Open()
    {
        if (_context != null)
            return;

        try
        {
            Directory.CreateDirectory(_directory);

            var creators = new ArrayList();
            creators.add(new MinimalArtifactInfoIndexCreator());
            creators.add(new JarFileContentsIndexCreator());
            creators.add(new MavenPluginArtifactInfoIndexCreator());

            _indexer ??= new DefaultIndexer(new DefaultSearchEngine(), new DefaultIndexerEngine(), new DefaultQueryCreator());
            _context = _indexer.createIndexingContext(_repositoryId + "-context", _repositoryId, null, new java.io.File(Path.Combine(_directory, "index")), _url, null, true, true, creators);
            _message = "";
        }
        catch (org.apache.lucene.store.LockObtainFailedException)
        {
            _message = "The index is in use by another instance of Visual Studio.";
        }
        catch (Exception e)
        {
            _message = $"Could not open the index: {e.Message}";
        }
    }

    void Download(MavenHttp http, RemoteRepository repository)
    {
        try
        {
            IndexingContext context;
            lock (_lock)
                context = _context!;

            var fetcher = new MavenIndexFetcher(http, repository, new java.io.File(Path.Combine(_directory, "download")), OnProgress);
            var updater = new DefaultIndexUpdater(new DefaultIncrementalHandler(), Collections.emptyList());
            updater.fetchAndUpdateIndex(new IndexUpdateRequest(context, fetcher));

            File.WriteAllText(CheckedFile, _url);
            lock (_lock)
                _message = "";
        }
        catch (Exception e)
        {
            lock (_lock)
            {
                _failed = DateTime.UtcNow;
                _message = $"Could not update the index: {e.Message}";
            }
        }
    }

    void OnProgress(string name, long received, long length)
    {
        // the properties that say which files to download are small
        if (name.EndsWith(".gz", StringComparison.OrdinalIgnoreCase) == false)
            return;

        string message;
        if (length > 0 && received >= length)
            message = "Updating the index...";
        else if (length > 0)
            message = $"Downloading the index: {received / 1048576} of {Math.Max(1, length / 1048576)} MB...";
        else
            message = $"Downloading the index: {received / 1048576} MB...";

        lock (_lock)
            _message = message;
    }

}
