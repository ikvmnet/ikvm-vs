using System;
using System.Collections.Generic;

using IKVM.VisualStudio.Host.Maven.Contracts;
using IKVM.VisualStudio.ProjectSystem.UI;

namespace IKVM.VisualStudio.Maven.UI;

/// <summary>
/// A repository of the project, as the Add Maven Reference dialog shows it: how it is searched, what is happening to
/// its index, and how the current search of it went.
/// </summary>
sealed class MavenRepositoryItem : ViewModelBase
{

    MavenServiceSearchStatus? _status;
    MavenRepositorySearchState _search;
    int _count;
    string _searchMessage = "";

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public MavenRepositoryItem(string id, string url)
    {
        Id = id;
        Url = url;
    }

    /// <summary>
    /// ID of the repository, which the dialog shows as its name.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// URL of the repository.
    /// </summary>
    public string Url { get; }

    /// <summary>
    /// How the repository is searched, once the service has said.
    /// </summary>
    public MavenServiceSearchStatus? Status
    {
        get => _status;
        set
        {
            _status = value;
            Changed();
        }
    }

    /// <summary>
    /// Whether searches include the repository.
    /// </summary>
    public bool IsSearchable => _status is { Method: not MavenServiceSearchMethod.None, IsReady: true };

    /// <summary>
    /// Sets how the current search of the repository is going.
    /// </summary>
    public void SetSearch(MavenRepositorySearchState state, int count = 0, string message = "")
    {
        _search = state;
        _count = count;
        _searchMessage = message;
        Changed();
    }

    /// <summary>
    /// Tells the dialog that everything it shows of the repository may have changed.
    /// </summary>
    void Changed()
    {
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(IsSearchable));
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(IsFound));
        OnPropertyChanged(nameof(IsFailed));
        OnPropertyChanged(nameof(IsIdle));
        OnPropertyChanged(nameof(IsUnsearchable));
        OnPropertyChanged(nameof(HasProgress));
        OnPropertyChanged(nameof(Progress));
        OnPropertyChanged(nameof(Suffix));
        OnPropertyChanged(nameof(ToolTip));
        OnPropertyChanged(nameof(AutomationName));
    }

    /// <summary>
    /// Whether the index of the repository is being downloaded or updated.
    /// </summary>
    bool IsIndexing =>_status is { IsUpdating: true };

    /// <summary>
    /// Whether something is happening that has no measure: checking the repository, searching it, or updating its
    /// index.
    /// </summary>
    public bool IsBusy => _status == null || _search == MavenRepositorySearchState.Searching || (_search == MavenRepositorySearchState.None && IsIndexing);

    /// <summary>
    /// Whether the current search of the repository completed, which the dialog shows with a check mark.
    /// </summary>
    public bool IsFound => _status != null && _search == MavenRepositorySearchState.Found;

    /// <summary>
    /// Whether the current search of the repository failed, which the dialog shows with a warning.
    /// </summary>
    public bool IsFailed => _status != null && _search == MavenRepositorySearchState.Failed;

    /// <summary>
    /// Whether the repository can be searched but nothing is happening to it, which the dialog shows with a dot.
    /// </summary>
    public bool IsIdle => _status != null && _search == MavenRepositorySearchState.None && IsIndexing == false && IsSearchable;

    /// <summary>
    /// Whether the repository cannot be searched, which the dialog shows with a hollow dot and dims it for.
    /// </summary>
    public bool IsUnsearchable => _status != null && _search == MavenRepositorySearchState.None && IsIndexing == false && IsSearchable == false;

    /// <summary>
    /// Whether the download of the index has a measure.
    /// </summary>
    public bool HasProgress => IsIndexing && _status!.Progress >= 0;

    /// <summary>
    /// How much of the index is downloaded, from 0 to 1, which the dialog shows as a bar.
    /// </summary>
    public double Progress => HasProgress ? _status!.Progress : 0;

    /// <summary>
    /// What follows the name: how many artifacts the search found, or how much of the index is downloaded.
    /// </summary>
    public string Suffix
    {
        get
        {
            if (IsFound)
                return _count.ToString();
            if (HasProgress && _search == MavenRepositorySearchState.None)
                return $"{_status!.Progress:P0}";

            return "";
        }
    }

    /// <summary>
    /// The tooltip of the repository: its URL, how it is searched, and how the current search of it went.
    /// </summary>
    public string ToolTip
    {
        get
        {
            var lines = new List<string>() { Url };
            if (_status == null)
                lines.Add("Checking how to search it...");
            else if (_status.Message.Length > 0)
                lines.Add(_status.Message);

            if (IsFound)
                lines.Add(_count == 1 ? "Found 1 artifact." : $"Found {_count} artifacts.");
            if (IsFailed)
                lines.Add($"Could not search: {_searchMessage}");

            return string.Join(Environment.NewLine, lines);
        }
    }

    /// <summary>
    /// What screen readers say of the repository: its ID and its tooltip, on one line.
    /// </summary>
    public string AutomationName => $"{Id}: {ToolTip.Replace(Environment.NewLine, " ")}";

}
