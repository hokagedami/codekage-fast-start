using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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

namespace FastStart.UI;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }
    private CancellationTokenSource? _searchCts;
    private readonly DispatcherTimer _searchDebounce;
    private AppWindow _appWindow;

    public MainWindow(MainViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();

        _appWindow = GetAppWindowForCurrentWindow();
        _appWindow.Resize(new SizeInt32(600, 700));

        ExtendsContentIntoTitleBar = true;
        SystemBackdrop = new MicaBackdrop();

        _searchDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _searchDebounce.Tick += SearchDebounce_Tick;

        Activated += (s, e) => SearchBox.Focus(FocusState.Programmatic);

        _ = LoadInitialDataAsync();
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
}
