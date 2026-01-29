using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Core.Services;
using FastStart.UI.Services;
using Microsoft.UI.Xaml.Controls;

namespace FastStart.UI.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private readonly ISearchService _searchService;
    private readonly IAppLauncher _launcher;
    private readonly IPinRepository _pinRepository;
    private readonly IRecentLaunchRepository _recentRepository;
    private readonly IAppRepository _appRepository;
    private readonly IconService _iconService;

    private string _searchQuery = string.Empty;
    private SearchResultViewModel? _selectedResult;
    private AppViewModel? _selectedApp;
    private bool _isLoading;

    private bool _isErrorBarOpen;
    private InfoBarSeverity _errorSeverity;
    private string _errorTitle = string.Empty;
    private string _errorMessage = string.Empty;

    public MainViewModel(
        ISearchService searchService,
        IAppLauncher launcher,
        IPinRepository pinRepository,
        IRecentLaunchRepository recentRepository,
        IAppRepository appRepository,
        IconService iconService)
    {
        _searchService = searchService;
        _launcher = launcher;
        _pinRepository = pinRepository;
        _recentRepository = recentRepository;
        _appRepository = appRepository;
        _iconService = iconService;

        SearchResults = new ObservableCollection<SearchResultViewModel>();
        PinnedApps = new ObservableCollection<AppViewModel>();
        RecentApps = new ObservableCollection<AppViewModel>();
        AllApps = new ObservableCollection<AppViewModel>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SearchQuery
    {
        get => _searchQuery;
        set
        {
            if (_searchQuery != value)
            {
                _searchQuery = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SearchQuery)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowDefaultView)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ShowSearchResults)));
            }
        }
    }

    public ObservableCollection<SearchResultViewModel> SearchResults { get; }
    public ObservableCollection<AppViewModel> PinnedApps { get; }
    public ObservableCollection<AppViewModel> RecentApps { get; }
    public ObservableCollection<AppViewModel> AllApps { get; }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
            }
        }
    }

    public AppViewModel? SelectedApp
    {
        get => _selectedApp;
        set
        {
            if (_selectedApp != value)
            {
                _selectedApp = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedApp)));
            }
        }
    }

    public SearchResultViewModel? SelectedResult
    {
        get => _selectedResult;
        set
        {
            if (_selectedResult != value)
            {
                _selectedResult = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedResult)));
            }
        }
    }

    public bool ShowDefaultView => string.IsNullOrEmpty(SearchQuery);
    public bool ShowSearchResults => !string.IsNullOrEmpty(SearchQuery);

    public bool IsErrorBarOpen
    {
        get => _isErrorBarOpen;
        set
        {
            if (_isErrorBarOpen != value)
            {
                _isErrorBarOpen = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsErrorBarOpen)));
            }
        }
    }

    public InfoBarSeverity ErrorSeverity
    {
        get => _errorSeverity;
        set
        {
            if (_errorSeverity != value)
            {
                _errorSeverity = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorSeverity)));
            }
        }
    }

    public string ErrorTitle
    {
        get => _errorTitle;
        set
        {
            if (_errorTitle != value)
            {
                _errorTitle = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorTitle)));
            }
        }
    }
    
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (_errorMessage != value)
            {
                _errorMessage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ErrorMessage)));
            }
        }
    }

    public async Task SearchAsync(string query, CancellationToken ct)
    {
        SearchResults.Clear();
        if (!string.IsNullOrEmpty(query))
        {
            var results = await _searchService.SearchAsync(query, ct);
            foreach (var result in results)
            {
                var vm = new SearchResultViewModel
                {
                    App = result.App,
                    Score = result.Score,
                    MatchKind = result.MatchKind
                };
                SearchResults.Add(vm);
                _ = LoadIcon(vm);
            }

            if (SearchResults.Any())
            {
                SelectedResult = SearchResults.First();
            }
        }
    }

    public async Task LoadPinnedAppsAsync(CancellationToken ct)
    {
        PinnedApps.Clear();
        var pins = await _pinRepository.GetPinsAsync(ct);
        foreach (var pin in pins)
        {
            if (pin.App is null) continue;
            var vm = new AppViewModel { App = pin.App, PinOrder = pin.Order, IsPinned = true };
            PinnedApps.Add(vm);
            _ = LoadIcon(vm);
        }
    }

    public async Task LoadRecentAppsAsync(CancellationToken ct)
    {
        RecentApps.Clear();
        var recents = await _recentRepository.GetRecentAsync(6, ct);
        foreach (var recent in recents)
        {
            if (recent.App is null) continue;
            var vm = new AppViewModel { App = recent.App };
            RecentApps.Add(vm);
            _ = LoadIcon(vm);
        }
    }

    public async Task LoadAllAppsAsync(CancellationToken ct)
    {
        IsLoading = true;
        try
        {
            AllApps.Clear();
            var apps = await _appRepository.GetAllAsync(ct);
            var pins = await _pinRepository.GetPinsAsync(ct);
            var pinnedIds = new HashSet<long>(pins.Select(p => p.ApplicationId));

            // Sort alphabetically by name
            var sorted = apps.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var app in sorted)
            {
                ct.ThrowIfCancellationRequested();
                var vm = new AppViewModel { App = app, IsPinned = pinnedIds.Contains(app.Id) };
                AllApps.Add(vm);
                _ = LoadIcon(vm);
            }

            if (AllApps.Any())
            {
                SelectedApp = AllApps.First();
            }
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LaunchAppAsync(AppInfo app)
    {
        var success = await _launcher.LaunchAsync(app, SearchQuery, CancellationToken.None);
        if (!success)
        {
            ErrorSeverity = InfoBarSeverity.Error;
            ErrorTitle = "Failed to launch application";
            ErrorMessage = $"Could not launch {app.Name}.";
            IsErrorBarOpen = true;
        }
    }

    private async Task LoadIcon(dynamic vm)
    {
        vm.Icon = await _iconService.GetIconAsync(vm.App);
    }

    public async Task PinAppAsync(AppInfo app, CancellationToken ct)
    {
        await _pinRepository.AddPinAsync(app.Id, ct);
        await LoadPinnedAppsAsync(ct);

        // Update IsPinned state in AllApps
        foreach (var vm in AllApps)
        {
            if (vm.App.Id == app.Id)
            {
                vm.IsPinned = true;
                break;
            }
        }
    }

    public async Task UnpinAppAsync(AppInfo app, CancellationToken ct)
    {
        await _pinRepository.RemovePinAsync(app.Id, ct);
        await LoadPinnedAppsAsync(ct);

        // Update IsPinned state in AllApps
        foreach (var vm in AllApps)
        {
            if (vm.App.Id == app.Id)
            {
                vm.IsPinned = false;
                break;
            }
        }
    }
}
