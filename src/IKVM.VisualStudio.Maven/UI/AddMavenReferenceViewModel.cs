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
using Microsoft.VisualStudio.Shell;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// State of the Add Maven Reference dialog: the repositories of the project and how each is searched, what is
/// searched for, what was found, and the artifact, version and scope to add. Follows the repositories of the project
/// as it is evaluated again, such as once IKVM.Maven.Sdk is added to it.
/// </summary>
sealed class AddMavenReferenceViewModel : ViewModelBase, IDisposable
{

    static readonly TimeSpan SearchDelay = TimeSpan.FromMilliseconds(400);

    /// <summary>
    /// How often to check on indexes being downloaded or updated.
    /// </summary>
    static readonly TimeSpan IndexPollInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How many artifacts to show.
    /// </summary>
    const int ResultCount = 40;

    readonly ConfiguredProject _project;
    readonly IReadOnlyDictionary<string, string> _existing;
    readonly CancellationTokenSource _closed = new CancellationTokenSource();
    readonly IDisposable _watch;
    IReadOnlyList<MavenRepository>? _repositories;
    CancellationTokenSource? _status;
    CancellationTokenSource? _search;
    CancellationTokenSource? _versions;
    string _searchText = "";
    bool _isSearching;
    string? _searchError;
    bool _searched;
    MavenSearchResult? _selected;
    string _version = "";
    string? _versionsStatus;
    bool _isLoadingVersions;
    string _scope = "";

    /// <summary>
    /// Starts following the repositories of a project, which already references some artifacts.
    /// </summary>
    /// <param name="project">The configured project whose repositories are used.</param>
    /// <param name="existing">The <c>groupId:artifactId</c> of the references already in the project, with their
    /// versions.</param>
    public AddMavenReferenceViewModel(ConfiguredProject project, IReadOnlyDictionary<string, string> existing)
    {
        _project = project;
        _existing = new Dictionary<string, string>(existing.ToDictionary(i => i.Key, i => i.Value), StringComparer.OrdinalIgnoreCase);
        Results.CollectionChanged += (s, e) => OnEmptyChanged();
        _watch = MavenProjectQueries.WatchRepositories(project, OnRepositoriesEvaluated);
    }

    /// <summary>
    /// Stops following the project and the indexes, once the dialog closes.
    /// </summary>
    public void Dispose()
    {
        _closed.Cancel();
        _watch.Dispose();
    }

    // repositories

    /// <summary>
    /// The repositories, as the settings reach them, and how each is searched.
    /// </summary>
    public ObservableCollection<MavenRepositoryItem> Repositories { get; } = new ObservableCollection<MavenRepositoryItem>();

    /// <summary>
    /// Whether the project names no repositories, as before IKVM.Maven.Sdk is restored.
    /// </summary>
    public bool HasNoRepositories => _repositories is { Count: 0 };

    /// <summary>
    /// Shows the repositories of the project each time it is evaluated, when they changed, and checks how each is
    /// searched; after the first time, searching again.
    /// </summary>
    void OnRepositoriesEvaluated(IReadOnlyList<MavenRepository> repositories)
    {
        _ = ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(_closed.Token);
            if (_repositories != null && _repositories.SequenceEqual(repositories))
                return;

            var first = _repositories == null;
            _repositories = repositories;
            OnPropertyChanged(nameof(HasNoRepositories));

            // the repositories the project names, until the service says which it reaches
            Repositories.Clear();
            foreach (var repository in repositories)
                Repositories.Add(new MavenRepositoryItem(repository.Id, repository.Url));

            await RefreshStatusAsync(searchAgain: first == false);
        });
    }

    /// <summary>
    /// Gets how each repository is searched, and keeps checking while indexes are downloaded or updated.
    /// </summary>
    async Task RefreshStatusAsync(bool searchAgain)
    {
        _status?.Cancel();
        var cts = _status = CancellationTokenSource.CreateLinkedTokenSource(_closed.Token);

        try
        {
            while (true)
            {
                var statuses = await MavenServiceClient.GetSearchStatusAsync(_project, cts.Token);
                cts.Token.ThrowIfCancellationRequested();
                ApplyStatuses(statuses);

                if (searchAgain)
                {
                    searchAgain = false;
                    _ = SearchAsync(SearchText);
                }

                if (statuses.Any(i => i.IsUpdating) == false)
                    break;

                await Task.Delay(IndexPollInterval, cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // the dialog closed, or the repositories changed
        }
    }

    /// <summary>
    /// Shows the repositories the service reaches, keeping what the current search of each found.
    /// </summary>
    void ApplyStatuses(IReadOnlyList<MavenServiceSearchStatus> statuses)
    {
        var wasSearchable = CanSearch;

        for (var i = 0; i < statuses.Count; i++)
        {
            var status = statuses[i];
            var index = IndexOf(status.RepositoryId);
            if (index < 0)
                Repositories.Insert(Math.Min(i, Repositories.Count), new MavenRepositoryItem(status.RepositoryId, status.Url) { Status = status });
            else
            {
                Repositories[index].Status = status;
                if (index != i && i < Repositories.Count)
                    Repositories.Move(index, i);
            }
        }

        // the repositories the project names, but the service reaches through a mirror
        while (Repositories.Count > statuses.Count)
            Repositories.RemoveAt(Repositories.Count - 1);

        OnPropertyChanged(nameof(CanSearch));
        OnPropertyChanged(nameof(SearchPlaceholder));
        OnEmptyChanged();

        // a repository whose index became ready may find what the others did not
        if (wasSearchable == false && CanSearch)
            _ = SearchAsync(SearchText);
    }

    /// <summary>
    /// Finds the position of a repository in <see cref="Repositories"/> by its ID, or -1.
    /// </summary>
    int IndexOf(string repositoryId)
    {
        for (var i = 0; i < Repositories.Count; i++)
            if (string.Equals(Repositories[i].Id, repositoryId, StringComparison.Ordinal))
                return i;

        return -1;
    }

    /// <summary>
    /// Whether any repository of the project can be searched: through its search service, or its index.
    /// </summary>
    public bool CanSearch => Repositories.Any(i => i.IsSearchable);

    /// <summary>
    /// What the search box shows when empty: whether it searches, or only takes coordinates.
    /// </summary>
    public string SearchPlaceholder => CanSearch ? "Search, or type groupId:artifactId[:version]" : "Type groupId:artifactId[:version]";

    // search

    /// <summary>
    /// The text of the search box. Changing it starts a new search.
    /// </summary>
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (Set(ref _searchText, value))
            {
                OnPropertyChanged(nameof(HasSearchText));
                _ = SearchAsync(value);
            }
        }
    }

    /// <summary>
    /// Whether the search box has text, which the dialog shows its clear button for.
    /// </summary>
    public bool HasSearchText => _searchText.Length > 0;

    /// <summary>
    /// The artifacts the list shows: typed coordinates first, then what the search found.
    /// </summary>
    public ObservableCollection<MavenSearchResult> Results { get; } = new ObservableCollection<MavenSearchResult>();

    /// <summary>
    /// Whether a search is waiting for repositories.
    /// </summary>
    public bool IsSearching
    {
        get => _isSearching;
        private set
        {
            if (Set(ref _isSearching, value))
                OnEmptyChanged();
        }
    }

    /// <summary>
    /// Typed coordinates come first, then, after a pause in typing, what each repository finds, as it finds it.
    /// </summary>
    async Task SearchAsync(string text)
    {
        _search?.Cancel();
        var cts = _search = CancellationTokenSource.CreateLinkedTokenSource(_closed.Token);

        var typed = ParseCoordinates(text);
        ShowResults(typed, Array.Empty<MavenSearchResult>());
        _searchError = null;
        _searched = false;
        foreach (var repository in Repositories)
            repository.SetSearch(MavenRepositorySearchState.None);

        if (CanSearch == false || text.Trim().Length < 2)
        {
            IsSearching = false;
            OnEmptyChanged();
            return;
        }

        try
        {
            IsSearching = true;
            await Task.Delay(SearchDelay, cts.Token);

            foreach (var repository in Repositories.Where(i => i.IsSearchable))
                repository.SetSearch(MavenRepositorySearchState.Searching);

            var failures = new List<string>();
            await MavenServiceClient.SearchAsync(_project, text, ResultCount, (update, found) =>
            {
                if (cts.IsCancellationRequested)
                    return;

                var index = IndexOf(update.RepositoryId);
                if (index >= 0)
                    Repositories[index].SetSearch(update.IsFailed ? MavenRepositorySearchState.Failed : MavenRepositorySearchState.Found, update.Count, update.Message);
                if (update.IsFailed)
                    failures.Add($"{update.RepositoryId}: {update.Message}");

                ShowResults(typed, found);
            }, cts.Token);
            cts.Token.ThrowIfCancellationRequested();

            // repositories the service did not search, such as those whose index is not ready
            foreach (var repository in Repositories.Where(i => i.IsBusy && i.Status != null))
                repository.SetSearch(MavenRepositorySearchState.None);

            if (failures.Count > 0 && Repositories.Any(i => i.IsFound) == false)
                _searchError = string.Join(Environment.NewLine, failures);

            _searched = true;
            IsSearching = false;
        }
        catch (OperationCanceledException)
        {
            // a newer search replaced this one
        }
        catch (Exception e)
        {
            _searchError = e.Message;
            _searched = true;
            foreach (var repository in Repositories.Where(i => i.IsBusy && i.Status != null))
                repository.SetSearch(MavenRepositorySearchState.Failed, message: e.Message);

            IsSearching = false;
        }
    }

    /// <summary>
    /// Shows typed coordinates and what was found, keeping the selected artifact as it is. Typed coordinates are
    /// selected; a found artifact only when chosen.
    /// </summary>
    void ShowResults(MavenSearchResult? typed, IReadOnlyList<MavenSearchResult> found)
    {
        var selected = Selected;
        Results.Clear();
        if (typed != null)
            Results.Add(typed with { IsTyped = true, ProjectVersion = GetProjectVersion(typed) });

        foreach (var result in found)
        {
            if (typed != null && string.Equals(result.Coordinates, typed.Coordinates, StringComparison.OrdinalIgnoreCase))
                continue;

            if (selected != null && string.Equals(result.Coordinates, selected.Coordinates, StringComparison.OrdinalIgnoreCase))
                Results.Add(selected);
            else
                Results.Add(result with { ProjectVersion = GetProjectVersion(result) });
        }

        if (Selected == null || Results.Contains(Selected) == false)
            Selected = Results.FirstOrDefault(i => i.IsTyped);
    }

    /// <summary>
    /// Gets the version the project references an artifact at, or <c>null</c> when it does not reference it.
    /// </summary>
    string? GetProjectVersion(MavenSearchResult result) => _existing.TryGetValue(result.Coordinates, out var version) ? version : null;

    // what the list shows when it has nothing

    /// <summary>
    /// Tells the dialog that what the list shows when it has nothing may have changed.
    /// </summary>
    void OnEmptyChanged()
    {
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(IsEmptyBusy));
        OnPropertyChanged(nameof(IsEmptyError));
        OnPropertyChanged(nameof(IsEmptyHint));
        OnPropertyChanged(nameof(EmptyText));
    }

    /// <summary>
    /// Whether the list has no artifacts, which the dialog shows a message in its place for.
    /// </summary>
    public bool IsEmpty => Results.Count == 0;

    /// <summary>
    /// Whether the empty list is waiting for the project or a search, which the dialog shows a spinner for.
    /// </summary>
    public bool IsEmptyBusy => IsEmpty && (IsSearching || _repositories == null);

    /// <summary>
    /// Whether the empty list is because the search failed, which the dialog shows a warning for.
    /// </summary>
    public bool IsEmptyError => IsEmpty && IsEmptyBusy == false && _searchError != null;

    /// <summary>
    /// Whether the empty list is neither busy nor failed, which the dialog shows a search image for.
    /// </summary>
    public bool IsEmptyHint => IsEmpty && IsEmptyBusy == false && IsEmptyError == false;

    /// <summary>
    /// The message the empty list shows: what is happening, why nothing was found, or what to do.
    /// </summary>
    public string EmptyText
    {
        get
        {
            if (_repositories == null)
                return "Reading the project...";
            if (IsSearching)
                return "Searching...";
            if (_searchError != null)
                return $"Could not search.{Environment.NewLine}{_searchError}";
            if (_searched)
                return $"Nothing found for “{_searchText.Trim()}”.";
            if (_repositories.Count == 0)
                return "The project has no Maven repositories yet." + Environment.NewLine + "They appear here once IKVM.Maven.Sdk is restored.";
            if (Repositories.Any(i => i.Status == null))
                return "Checking how to search the repositories...";
            if (CanSearch == false)
                return "None of the repositories of the project can be searched." + Environment.NewLine + "Type groupId:artifactId[:version].";

            return "Search the repositories of the project," + Environment.NewLine + "or type groupId:artifactId[:version].";
        }
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

    /// <summary>
    /// The artifact to add. Changing it lists its versions.
    /// </summary>
    public MavenSearchResult? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value))
            {
                OnPropertyChanged(nameof(HasSelected));
                OnPropertyChanged(nameof(IsSelectedInProject));
                OnPropertyChanged(nameof(CanEditSelected));
                OnPropertyChanged(nameof(Problem));
                OnPropertyChanged(nameof(CanAdd));
                _ = LoadVersionsAsync(value);
            }
        }
    }

    /// <summary>
    /// Whether an artifact is selected, which the dialog shows its version and scope for.
    /// </summary>
    public bool HasSelected => _selected != null;

    /// <summary>
    /// The versions of the selected artifact in the repositories of the project, newest first.
    /// </summary>
    public ObservableCollection<string> Versions { get; } = new ObservableCollection<string>();

    /// <summary>
    /// The version to add the artifact at, chosen or typed.
    /// </summary>
    public string Version
    {
        get => _version;
        set
        {
            if (Set(ref _version, value?.Trim() ?? ""))
                OnPropertyChanged(nameof(CanAdd));
        }
    }

    /// <summary>
    /// What the dialog shows under the version: that versions are being found, or how many were.
    /// </summary>
    public string? VersionsStatus
    {
        get => _versionsStatus;
        set => Set(ref _versionsStatus, value);
    }

    /// <summary>
    /// Whether the versions of the selected artifact are being found, which the dialog shows a spinner for.
    /// </summary>
    public bool IsLoadingVersions
    {
        get => _isLoadingVersions;
        private set => Set(ref _isLoadingVersions, value);
    }

    /// <summary>
    /// Lists the versions of the selected artifact with Maven Resolver, choosing the newest release,
    /// or the version typed.
    /// </summary>
    async Task LoadVersionsAsync(MavenSearchResult? selected)
    {
        _versions?.Cancel();
        var cts = _versions = CancellationTokenSource.CreateLinkedTokenSource(_closed.Token);

        Versions.Clear();
        Version = selected?.LatestVersion is { Length: > 0 } latest ? latest : _typedVersion ?? "";
        if (selected == null || IsSelectedInProject)
        {
            VersionsStatus = null;
            IsLoadingVersions = false;
            return;
        }

        VersionsStatus = "Finding versions...";
        IsLoadingVersions = true;
        var versions = await MavenServiceClient.GetVersionsAsync(_project, selected.GroupId, selected.ArtifactId, cts.Token);
        if (cts.IsCancellationRequested)
            return;

        foreach (var version in versions)
            Versions.Add(version);

        if (selected.LatestVersion.Length == 0 && _typedVersion == null)
            Version = versions.FirstOrDefault(IsRelease) ?? versions.FirstOrDefault() ?? "";

        IsLoadingVersions = false;
        VersionsStatus = versions.Count switch
        {
            0 => "No versions found in the repositories of the project. Type one.",
            1 => "1 version.",
            _ => $"{versions.Count} versions.",
        };
    }

    /// <summary>
    /// Matches the qualifiers Maven gives versions that are not releases, such as <c>-alpha1</c>, <c>-RC2</c>,
    /// <c>-M3</c> or <c>-SNAPSHOT</c>.
    /// </summary>
    static readonly Regex PreReleaseQualifier = new Regex(@"[-.](alpha|a|beta|b|rc|cr|m|milestone|ea|preview|snapshot)[-.]?\d*([-.]|$)", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    /// <summary>
    /// Whether a version is a release, without a qualifier such as <c>-RC2</c> or <c>-SNAPSHOT</c>.
    /// </summary>
    static bool IsRelease(string version) => PreReleaseQualifier.IsMatch(version) == false;

    /// <summary>
    /// The scopes the scope box offers.
    /// </summary>
    public IReadOnlyList<string> ScopeOptions => MavenDependencyEntry.Scopes;

    /// <summary>
    /// The scope to add the artifact with, or empty for the default, compile.
    /// </summary>
    public string Scope
    {
        get => _scope;
        set => Set(ref _scope, value ?? "");
    }

    /// <summary>
    /// Whether the project already references the selected artifact, which cannot be added again.
    /// </summary>
    public bool IsSelectedInProject => _selected != null && _existing.ContainsKey(_selected.Coordinates);

    /// <summary>
    /// Whether the selected artifact can be given a version and scope to add it at.
    /// </summary>
    public bool CanEditSelected => _selected != null && IsSelectedInProject == false;

    /// <summary>
    /// Why the selected artifact cannot be added, if it cannot.
    /// </summary>
    public string? Problem => IsSelectedInProject && _existing.TryGetValue(_selected!.Coordinates, out var version)
        ? $"The project already references this artifact{(version.Length > 0 ? $" at {version}" : "")}. Change its version in Manage IKVM Dependencies."
        : null;

    /// <summary>
    /// Whether an artifact is selected with a version, and can be added, which enables the Add button.
    /// </summary>
    public bool CanAdd => _selected != null && _version.Length > 0 && Problem == null;

}
