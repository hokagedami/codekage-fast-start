using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using FastStart.Avalonia.Services;
using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Core.Services;
using Microsoft.Extensions.Logging;

namespace FastStart.Avalonia.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISearchService _searchService;
    private readonly IAppRepository _appRepository;
    private readonly IPinRepository _pinRepository;
    private readonly IRecentLaunchRepository _recentLaunchRepository;
    private readonly IAppLauncher _appLauncher;
    private readonly IconService _iconService;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _showSearchResults;

    [ObservableProperty]
    private bool _showDefaultView = true;

    [ObservableProperty]
    private AppViewModel? _selectedResult;

    public ObservableCollection<AppViewModel> SearchResults { get; } = new();
    public ObservableCollection<AppViewModel> PinnedApps { get; } = new();
    public ObservableCollection<AppViewModel> RecentApps { get; } = new();
    public ObservableCollection<AppViewModel> AllApps { get; } = new();

    public MainViewModel(
        ISearchService searchService,
        IAppRepository appRepository,
        IPinRepository pinRepository,
        IRecentLaunchRepository recentLaunchRepository,
        IAppLauncher appLauncher,
        IconService iconService,
        ILogger<MainViewModel> logger)
    {
        _searchService = searchService;
        _appRepository = appRepository;
        _pinRepository = pinRepository;
        _recentLaunchRepository = recentLaunchRepository;
        _appLauncher = appLauncher;
        _iconService = iconService;
        _logger = logger;
    }

    partial void OnSearchQueryChanged(string value)
    {
        ShowSearchResults = !string.IsNullOrWhiteSpace(value);
        ShowDefaultView = string.IsNullOrWhiteSpace(value);
    }

    public async Task SearchAsync(string query, CancellationToken ct)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                SearchResults.Clear();
                return;
            }

            var results = await _searchService.SearchAsync(query, ct);

            SearchResults.Clear();
            foreach (var result in results.Take(20))
            {
                var vm = new AppViewModel { App = result.App };
                SearchResults.Add(vm);
                _ = LoadIconAsync(vm);
            }

            if (SearchResults.Count > 0)
            {
                SelectedResult = SearchResults[0];
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when search is cancelled
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Search failed for query: {Query}", query);
        }
    }

    public async Task LoadPinnedAppsAsync(CancellationToken ct)
    {
        try
        {
            var pins = await _pinRepository.GetPinsAsync(ct);

            PinnedApps.Clear();
            foreach (var pin in pins.OrderBy(p => p.Position))
            {
                if (pin.App is not null)
                {
                    var vm = new AppViewModel { App = pin.App };
                    PinnedApps.Add(vm);
                    _ = LoadIconAsync(vm);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load pinned apps.");
        }
    }

    public async Task LoadRecentAppsAsync(CancellationToken ct)
    {
        try
        {
            var recent = await _recentLaunchRepository.GetRecentAsync(6, ct);

            RecentApps.Clear();
            foreach (var launch in recent)
            {
                if (launch.App is not null)
                {
                    var vm = new AppViewModel { App = launch.App };
                    RecentApps.Add(vm);
                    _ = LoadIconAsync(vm);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load recent apps.");
        }
    }

    public async Task LoadAllAppsAsync(CancellationToken ct)
    {
        try
        {
            IsLoading = true;
            var apps = await _appRepository.GetAllAsync(ct);

            AllApps.Clear();
            foreach (var app in apps.OrderBy(a => a.Name))
            {
                var vm = new AppViewModel { App = app };
                AllApps.Add(vm);
                _ = LoadIconAsync(vm);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load all apps.");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LaunchAppAsync(AppInfo app)
    {
        try
        {
            await _appLauncher.LaunchAsync(app, null, CancellationToken.None);
            await _recentLaunchRepository.AddAsync(new RecentLaunchInfo
            {
                ApplicationId = app.Id,
                LaunchedAtUtc = DateTimeOffset.UtcNow
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch app: {AppName}", app.Name);
        }
    }

    public async Task PinAppAsync(AppInfo app, CancellationToken ct)
    {
        try
        {
            await _pinRepository.AddPinAsync(app.Id, ct);
            await LoadPinnedAppsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to pin app: {AppName}", app.Name);
        }
    }

    public async Task UnpinAppAsync(AppInfo app, CancellationToken ct)
    {
        try
        {
            await _pinRepository.RemovePinAsync(app.Id, ct);
            await LoadPinnedAppsAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to unpin app: {AppName}", app.Name);
        }
    }

    private async Task LoadIconAsync(AppViewModel vm)
    {
        try
        {
            vm.Icon = await _iconService.GetIconAsync(vm.App);
        }
        catch
        {
            // Ignore icon loading errors
        }
    }
}

public partial class AppViewModel : ObservableObject
{
    [ObservableProperty]
    private AppInfo _app = null!;

    [ObservableProperty]
    private Bitmap? _icon;
}
