using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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

    readonly ConfiguredProject _project;
    readonly ISet<string> _existing;
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
        OnPropertyChanged(nameof(CanSearch));
        OnPropertyChanged(nameof(SearchPlaceholder));
    }

    /// <summary>
    /// Where versions come from.
    /// </summary>
    public string RepositoriesText => _repositories.Count == 0 ? "" : "Versions from " + string.Join(", ", _repositories.Select(i => i.Id)) + ".";

    /// <summary>
    /// Whether the project uses Maven Central, the only repository that can be searched.
    /// </summary>
    public bool CanSearch => _repositories.Any(i => i.IsCentral);

    public string SearchPlaceholder => CanSearch ? "Search Maven Central, or type groupId:artifactId" : "Type groupId:artifactId";

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
    /// Typed coordinates come first, then, after a pause in typing, what Maven Central finds.
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
            SearchStatus = "Searching Maven Central...";

            var found = await MavenCentralSearch.SearchAsync(text, cts.Token);
            cts.Token.ThrowIfCancellationRequested();

            foreach (var result in found.Where(i => typed == null || string.Equals(i.Coordinates, typed.Coordinates, StringComparison.OrdinalIgnoreCase) == false))
                Results.Add(result);

            SearchStatus = Results.Count == 0 ? "Nothing found." : null;
            SelectFirst();
        }
        catch (OperationCanceledException)
        {
            // a newer search replaced this one
        }
        catch (Exception e)
        {
            SearchStatus = $"Could not search Maven Central: {e.Message}";
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
    /// Lists the versions of the selected artifact through the project's IKVM.Maven.Sdk, choosing the newest release,
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
        var versions = await MavenProjectQueries.GetVersionsAsync(_project, selected.GroupId, selected.ArtifactId, cts.Token);
        if (cts.IsCancellationRequested)
            return;

        foreach (var version in versions)
            Versions.Add(version);

        if (selected.LatestVersion.Length == 0 && _typedVersion == null)
            Version = versions.FirstOrDefault(IsRelease) ?? versions.FirstOrDefault() ?? "";

        VersionsStatus = versions.Count == 0 ? "No versions found in the repositories of the project. Type one, or build the project once IKVM.Maven.Sdk is restored." : $"{versions.Count} versions.";
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
