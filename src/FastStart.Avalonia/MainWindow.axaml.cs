using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using FastStart.Avalonia.ViewModels;
using FastStart.Core.Repositories;

namespace FastStart.Avalonia;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly IPreferencesRepository _preferencesRepository;
    private CancellationTokenSource? _searchCts;
    private readonly DispatcherTimer _searchDebounce;
    private bool _dataLoaded;

    public MainWindow(MainViewModel viewModel, IPreferencesRepository preferencesRepository)
    {
        _viewModel = viewModel;
        _preferencesRepository = preferencesRepository;
        DataContext = _viewModel;

        InitializeComponent();

        PositionWindowAboveTaskbar();

        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _searchDebounce.Tick += SearchDebounce_Tick;

        // Subscribe to SearchQuery changes via PropertyChanged
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;

        Opened += OnOpened;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.SearchQuery))
        {
            _searchDebounce.Stop();
            _searchDebounce.Start();
        }
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        SearchBox.Focus();

        if (!_dataLoaded)
        {
            _dataLoaded = true;
            _ = LoadInitialDataAsync();
        }
    }

    private void PositionWindowAboveTaskbar()
    {
        var screen = Screens.Primary;
        if (screen is null) return;

        var workArea = screen.WorkingArea;
        var x = workArea.X + 12;
        var y = workArea.Y + workArea.Height - (int)Height - 12;

        Position = new PixelPoint(x, y);
    }

    private async Task LoadInitialDataAsync()
    {
        await _viewModel.LoadAllAppsAsync(CancellationToken.None);
        await _viewModel.LoadPinnedAppsAsync(CancellationToken.None);
        await _viewModel.LoadRecentAppsAsync(CancellationToken.None);
    }

    private async void SearchDebounce_Tick(object? sender, EventArgs e)
    {
        _searchDebounce.Stop();
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        await _viewModel.SearchAsync(_viewModel.SearchQuery, _searchCts.Token);
    }

    private async void SearchBox_KeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                if (_viewModel.SelectedResult is not null)
                {
                    await _viewModel.LaunchAppAsync(_viewModel.SelectedResult.App);
                    Hide();
                }
                break;
            case Key.Down:
                if (_viewModel.SearchResults.Any())
                {
                    var selectedIndex = _viewModel.SearchResults.IndexOf(_viewModel.SelectedResult);
                    if (selectedIndex < _viewModel.SearchResults.Count - 1)
                    {
                        _viewModel.SelectedResult = _viewModel.SearchResults[selectedIndex + 1];
                    }
                }
                break;
            case Key.Up:
                if (_viewModel.SearchResults.Any())
                {
                    var selectedIndex = _viewModel.SearchResults.IndexOf(_viewModel.SelectedResult);
                    if (selectedIndex > 0)
                    {
                        _viewModel.SelectedResult = _viewModel.SearchResults[selectedIndex - 1];
                    }
                }
                break;
            case Key.Escape:
                Hide();
                break;
        }
    }

    private async void SearchResult_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is AppViewModel app)
        {
            await _viewModel.LaunchAppAsync(app.App);
            Hide();
        }
    }

    private async void PinnedApp_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is AppViewModel app)
        {
            await _viewModel.LaunchAppAsync(app.App);
            Hide();
        }
    }

    private async void RecentApp_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is AppViewModel app)
        {
            await _viewModel.LaunchAppAsync(app.App);
            Hide();
        }
    }

    private async void AllApp_DoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is ListBox listBox && listBox.SelectedItem is AppViewModel app)
        {
            await _viewModel.LaunchAppAsync(app.App);
            Hide();
        }
    }

    private void AllApps_Click(object? sender, RoutedEventArgs e)
    {
        AllAppsSection.IsVisible = true;
    }

    private void BackToPinned_Click(object? sender, RoutedEventArgs e)
    {
        AllAppsSection.IsVisible = false;
    }

    private void SettingsButton_Click(object? sender, RoutedEventArgs e)
    {
        // TODO: Implement settings page
    }

    public void ToggleVisibility()
    {
        if (IsVisible)
        {
            Hide();
        }
        else
        {
            ResetViewState();
            PositionWindowAboveTaskbar();
            Show();
            Activate();
            SearchBox.Focus();
        }
    }

    private void ResetViewState()
    {
        _viewModel.SearchQuery = string.Empty;
        AllAppsSection.IsVisible = false;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        // Hide instead of close
        e.Cancel = true;
        Hide();
    }
}
