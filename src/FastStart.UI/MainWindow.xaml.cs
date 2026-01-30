using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FastStart.Core.Repositories;
using FastStart.Native;
using FastStart.UI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Windowing;
using Windows.Graphics;
using Windows.System;
using WinRT.Interop;
using Microsoft.UI.Composition.SystemBackdrops;

namespace FastStart.UI;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private readonly IPreferencesRepository _preferencesRepository;
    private readonly GlobalKeyboardHook _keyboardHook;
    private CancellationTokenSource? _searchCts;
    private readonly DispatcherTimer _searchDebounce;
    private AppWindow _appWindow;
    private SettingsPage? _settingsPage;

    public MainWindow(MainViewModel viewModel, IPreferencesRepository preferencesRepository, GlobalKeyboardHook keyboardHook)
    {
        ViewModel = viewModel;
        _preferencesRepository = preferencesRepository;
        _keyboardHook = keyboardHook;
        this.InitializeComponent();

        _appWindow = GetAppWindowForCurrentWindow();
        _appWindow.Resize(new SizeInt32(600, 700));
        PositionWindowAboveTaskbar();

        // Make window borderless like Windows 11 Start Menu
        if (_appWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(false, false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
        }

        SystemBackdrop = new MicaBackdrop();

        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _searchDebounce.Tick += SearchDebounce_Tick;

        Activated += OnFirstActivated;
    }

    private bool _dataLoaded;

    private void OnFirstActivated(object sender, WindowActivatedEventArgs e)
    {
        SearchBox.Focus(FocusState.Programmatic);

        // Load data only once, after window is visible
        if (!_dataLoaded)
        {
            _dataLoaded = true;
            _ = LoadInitialDataAsync();
        }
    }

    private void PositionWindowAboveTaskbar()
    {
        // Get screen dimensions
        var displayArea = DisplayArea.GetFromWindowId(_appWindow.Id, DisplayAreaFallback.Primary);
        var workArea = displayArea.WorkArea;

        // Position at bottom-left, above taskbar
        var x = workArea.X + 12;
        var y = workArea.Y + workArea.Height - 700 - 12;
        _appWindow.Move(new Windows.Graphics.PointInt32(x, y));
    }

    private AppWindow GetAppWindowForCurrentWindow()
    {
        IntPtr hWnd = WindowNative.GetWindowHandle(this);
        WindowId wndId = Win32Interop.GetWindowIdFromWindow(hWnd);
        return AppWindow.GetFromWindowId(wndId);
    }

    private async Task LoadInitialDataAsync()
    {
        await ViewModel.LoadAllAppsAsync(CancellationToken.None);
        await ViewModel.LoadPinnedAppsAsync(CancellationToken.None);
        await ViewModel.LoadRecentAppsAsync(CancellationToken.None);
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _searchDebounce.Stop();
        _searchDebounce.Start();
    }

    private async void SearchDebounce_Tick(object? sender, object e)
    {
        _searchDebounce.Stop();
        _searchCts?.Cancel();
        _searchCts = new CancellationTokenSource();
        await ViewModel.SearchAsync(ViewModel.SearchQuery, _searchCts.Token);
    }

    private async void SearchBox_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        switch (e.Key)
        {
            case VirtualKey.Enter:
                if (ViewModel.SelectedResult is not null)
                {
                    await ViewModel.LaunchAppAsync(ViewModel.SelectedResult.App);
                    Close();
                }
                break;
            case VirtualKey.Down:
                if (ViewModel.SearchResults.Any())
                {
                    var selectedIndex = ViewModel.SearchResults.IndexOf(ViewModel.SelectedResult);
                    if (selectedIndex < ViewModel.SearchResults.Count - 1)
                    {
                        ViewModel.SelectedResult = ViewModel.SearchResults[selectedIndex + 1];
                    }
                }
                break;
            case VirtualKey.Up:
                if (ViewModel.SearchResults.Any())
                {
                    var selectedIndex = ViewModel.SearchResults.IndexOf(ViewModel.SelectedResult);
                    if (selectedIndex > 0)
                    {
                        ViewModel.SelectedResult = ViewModel.SearchResults[selectedIndex - 1];
                    }
                }
                break;
            case VirtualKey.Escape:
                Close();
                break;
        }
    }

    private async void SearchResult_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is SearchResultViewModel result)
        {
            await ViewModel.LaunchAppAsync(result.App);
            Close();
        }
    }

    private async void PinnedApp_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AppViewModel app)
        {
            await ViewModel.LaunchAppAsync(app.App);
            Close();
        }
    }

    private async void RecentApp_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AppViewModel app)
        {
            await ViewModel.LaunchAppAsync(app.App);
            Close();
        }
    }

    private async void AllApp_Click(object sender, ItemClickEventArgs e)
    {
        if (e.ClickedItem is AppViewModel app)
        {
            await ViewModel.LaunchAppAsync(app.App);
            Close();
        }
    }

    private void AllApps_Click(object sender, RoutedEventArgs e)
    {
        AllAppsSection.Visibility = Visibility.Visible;
    }

    private void BackToPinned_Click(object sender, RoutedEventArgs e)
    {
        AllAppsSection.Visibility = Visibility.Collapsed;
    }

    private async void RecentAppButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is AppViewModel app)
        {
            await ViewModel.LaunchAppAsync(app.App);
            Close();
        }
    }

    private async void PinApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is AppViewModel app)
        {
            await ViewModel.PinAppAsync(app.App, CancellationToken.None);
        }
    }

    private async void UnpinApp_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem item && item.Tag is AppViewModel app)
        {
            await ViewModel.UnpinAppAsync(app.App, CancellationToken.None);
        }
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        ShowSettings();
    }

    private void ShowSettings()
    {
        if (_settingsPage is null)
        {
            _settingsPage = new SettingsPage(_preferencesRepository, _keyboardHook);
            _settingsPage.BackRequested += (s, e) => HideSettings();
        }

        SettingsContainer.Content = _settingsPage;
        SettingsContainer.Visibility = Visibility.Visible;
        MainContent.Visibility = Visibility.Collapsed;
    }

    private void HideSettings()
    {
        SettingsContainer.Visibility = Visibility.Collapsed;
        MainContent.Visibility = Visibility.Visible;
        SearchBox.Focus(FocusState.Programmatic);
    }

    public void ToggleVisibility()
    {
        if (_appWindow.IsVisible)
        {
            _appWindow.Hide();
        }
        else
        {
            ResetViewState();
            PositionWindowAboveTaskbar();
            _appWindow.Show();
            Activate();
            SearchBox.Focus(FocusState.Programmatic);
        }
    }

    public void ShowWindow()
    {
        ResetViewState();
        PositionWindowAboveTaskbar();
        _appWindow.Show();
        Activate();
        SearchBox.Focus(FocusState.Programmatic);
    }

    private void ResetViewState()
    {
        // Clear search query and results
        ViewModel.SearchQuery = string.Empty;

        // Hide settings if open
        if (SettingsContainer.Visibility == Visibility.Visible)
        {
            HideSettings();
        }

        // Collapse All Apps section if expanded
        AllAppsSection.Visibility = Visibility.Collapsed;
    }

    public void HideWindow()
    {
        _appWindow.Hide();
    }
}
