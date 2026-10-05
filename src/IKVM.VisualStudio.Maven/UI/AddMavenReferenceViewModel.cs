using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using IKVM.VisualStudio.Host.Maven.Contracts;
using IKVM.VisualStudio.ProjectSystem.UI;

using Microsoft.VisualStudio.ProjectSystem;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// State of the Add Maven Reference dialog: what is searched for, what was found, and the artifact, version and scope
/// to add.
/// </summary>
sealed class AddMavenReferenceViewModel : ViewModelBase
{

    static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(400);

    /// <summary>
    /// How often to check on indexes being downloaded or updated.
    /// </summary>
    static readonly TimeSpan IndexPollInterval = TimeSpan.FromSeconds(2);

    readonly ConfiguredProject _project;
    readonly ISet<string> _existing;
    readonly CancellationTokenSource _closed = new CancellationTokenSource();
    IReadOnlyList<MavenRepository> _repositories = Array.Empty<MavenRepository>();
    CancellationTokenSource? _search;
    CancellationTokenSource? _versions;
    string _searchText = "";
    string? _searchStatus;
    MavenSearchResult? _selected;
    string _version = "";
    string? _versionsStatus;
    string _scope = "";

    /// <param name="project">The configured project whose repositories are used.</param>
    /// <param name="existing">The <c>groupId:artifactId</c> of the references already in the project.</param>
    public AddMavenReferenceViewModel(ConfiguredProject project, IEnumerable<string> existing)
    {
        _project = project;
        _existing = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        _ = LoadRepositoriesAsync();
    }

    async Task LoadRepositoriesAsync()
    {
        _repositories = await MavenProjectQueries.GetRepositoriesAsync(_project);
        OnPropertyChanged(nameof(RepositoriesText));

        // how each repository is searched, until the indexes being downloaded or updated are
        try
        {
            while (true)
            {
                var statuses = await MavenServiceClient.GetSearchStatusAsync(_project, _closed.Token);
                _canSearch = statuses.Any(i => i.Method != MavenServiceSearchMethod.None);
                IndexStatus = string.Join(Environment.NewLine, statuses.Where(i => i.Method == MavenServiceSearchMethod.Index && (i.IsUpdating || i.IsReady == false)).Select(i => $"{i.RepositoryId}: {i.Message}"));
                OnPropertyChanged(nameof(CanSearch));
                OnPropertyChanged(nameof(SearchPlaceholder));

                if (statuses.Any(i => i.IsUpdating) == false)
                    break;

                await Task.Delay(IndexPollInterval, _closed.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // the dialog closed
        }
    }

    /// <summary>
    /// Stops checking on indexes, once the dialog closes.
    /// </summary>
    public void Close()
    {
        _closed.Cancel();
    }

    /// <summary>
    /// Where versions come from.
    /// </summary>
    public string RepositoriesText => _repositories.Count == 0 ? "The project has no Maven repositories." : "Versions from " + string.Join(", ", _repositories.Select(i => i.Id)) + ".";

    /// <summary>
    /// Whether any repository of the project can be searched: through its search service, or its index.
    /// </summary>
    public bool CanSearch => _canSearch;

    bool _canSearch;

    public string SearchPlaceholder => CanSearch ? "Search, or type groupId:artifactId" : "Type groupId:artifactId";

    /// <summary>
    /// What is happening to the indexes of the repositories searched through them, until they are ready.
    /// </summary>
    public string? IndexStatus
    {
        get => _indexStatus;
        private set
        {
            if (Set(ref _indexStatus, string.IsNullOrEmpty(value) ? null : value))
                OnPropertyChanged(nameof(HasIndexStatus));
        }
    }

    public bool HasIndexStatus => _indexStatus != null;

    string? _indexStatus;

    // search

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
                _ = SearchAsync(value);
        }
    }

    public ObservableCollection<MavenSearchResult> Results { get; } = new ObservableCollection<MavenSearchResult>();

    /// <summary>
    /// What the search is doing, or why it found nothing.
    /// </summary>
    public string? SearchStatus
    {
        get => _searchStatus;
        set => Set(ref _searchStatus, value);
    }

    /// <summary>
    /// Typed coordinates come first, then, after a pause in typing, what a search of the repositories finds.
    /// </summary>
    async Task SearchAsync(string text)
    {
        _search?.Cancel();
        var cts = _search = new CancellationTokenSource();

        Results.Clear();
        var typed = ParseCoordinates(text);
        if (typed != null)
            Results.Add(typed);

        if (CanSearch == false || text.Trim().Length < 2)
        {
            SearchStatus = null;
            SelectFirst();
            return;
        }

        try
        {
            await Task.Delay(SearchDelay, cts.Token);
            SearchStatus = "Searching...";

            // each repository adds what it finds as it completes; what is selected stays selected
            await MavenServiceClient.SearchAsync(_project, text, 40, found =>
            {
                if (cts.IsCancellationRequested)
                    return;

                var selected = Selected;
                Results.Clear();
                if (typed != null)
                    Results.Add(typed);

                foreach (var result in found.Where(i => typed == null || string.Equals(i.Coordinates, typed.Coordinates, StringComparison.OrdinalIgnoreCase) == false))
                    Results.Add(selected != null && string.Equals(result.Coordinates, selected.Coordinates, StringComparison.OrdinalIgnoreCase) ? selected : result);

                SelectFirst();
            }, cts.Token);
            cts.Token.ThrowIfCancellationRequested();

            SearchStatus = Results.Count == 0 ? "Nothing found." : null;
            SelectFirst();
        }
        catch (OperationCanceledException)
        {
            // a newer search replaced this one
        }
        catch (Exception e)
        {
            SearchStatus = $"Could not search: {e.Message}";
        }
    }

    void SelectFirst()
    {
        if (Selected == null || Results.Contains(Selected) == false)
            Selected = Results.FirstOrDefault();
    }

    /// <summary>
    /// Reads <c>groupId:artifactId[:version]</c>, its version taken as the one to add.
    /// </summary>
    MavenSearchResult? ParseCoordinates(string text)
    {
        _typedVersion = null;
        var parts = text.Trim().Split(':');
        if (parts.Length is not (2 or 3) || parts.Any(i => i.Trim().Length == 0 || i.Contains(' ')))
            return null;

        if (parts.Length == 3)
            _typedVersion = parts[2].Trim();

        return new MavenSearchResult(parts[0].Trim(), parts[1].Trim(), "");
    }

    string? _typedVersion;

    // the artifact to add

    public MavenSearchResult? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value))
            {
                OnPropertyChanged(nameof(HasSelected));
                OnPropertyChanged(nameof(Problem));
                OnPropertyChanged(nameof(CanAdd));
                _ = LoadVersionsAsync(value);
            }
        }
    }

    public bool HasSelected => _selected != null;

    public ObservableCollection<string> Versions { get; } = new ObservableCollection<string>();

    public string Version
    {
        get => _version;
        set
        {
            if (Set(ref _version, value?.Trim() ?? ""))
                OnPropertyChanged(nameof(CanAdd));
        }
    }

    public string? VersionsStatus
    {
        get => _versionsStatus;
        set => Set(ref _versionsStatus, value);
    }

    /// <summary>
    /// Lists the versions of the selected artifact with Maven Resolver, choosing the newest release,
    /// or the version typed.
    /// </summary>
    async Task LoadVersionsAsync(MavenSearchResult? selected)
    {
        _versions?.Cancel();
        var cts = _versions = new CancellationTokenSource();

        Versions.Clear();
        Version = selected?.LatestVersion is { Length: > 0 } latest ? latest : _typedVersion ?? "";
        if (selected == null)
        {
            VersionsStatus = null;
            return;
        }

        VersionsStatus = "Finding versions...";
        var versions = await MavenServiceClient.GetVersionsAsync(_project, selected.GroupId, selected.ArtifactId, cts.Token);
        if (cts.IsCancellationRequested)
            return;

        foreach (var version in versions)
            Versions.Add(version);

        if (selected.LatestVersion.Length == 0 && _typedVersion == null)
            Version = versions.FirstOrDefault(IsRelease) ?? versions.FirstOrDefault() ?? "";

        VersionsStatus = versions.Count == 0 ? "No versions found in the repositories of the project. Type one." : $"{versions.Count} versions.";
    }

    /// <summary>
    /// Matches the qualifiers Maven gives versions that are not releases, such as <c>-alpha1</c>, <c>-RC2</c>,
    /// <c>-M3</c> or <c>-SNAPSHOT</c>.
    /// </summary>
    static readonly Regex PreReleaseQualifier = new Regex(@"[-.](alpha|a|beta|b|rc|cr|m|milestone|ea|preview|snapshot)[-.]?\d*([-.]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    static bool IsRelease(string version) => PreReleaseQualifier.IsMatch(version) == false;

    public IReadOnlyList<string> ScopeOptions => MavenDependencyEntry.Scopes;

    public string Scope
    {
        get => _scope;
        set => Set(ref _scope, value ?? "");
    }

    /// <summary>
    /// Why the selected artifact cannot be added, if it cannot.
    /// </summary>
    public string? Problem => _selected != null && _existing.Contains(_selected.Coordinates) ? $"{_selected.Coordinates} is already referenced. Change its version in Manage IKVM Dependencies." : null;

    public bool CanAdd => _selected != null && _version.Length > 0 && Problem == null;

}
