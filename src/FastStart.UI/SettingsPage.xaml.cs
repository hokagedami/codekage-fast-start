using System;
using System.Threading;
using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Native;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FastStart.UI;

public sealed partial class SettingsPage : Page
{
    private readonly IPreferencesRepository _preferencesRepository;
    private readonly GlobalKeyboardHook _keyboardHook;
    private bool _isLoading = true;

    public event EventHandler? BackRequested;

    public SettingsPage(IPreferencesRepository preferencesRepository, GlobalKeyboardHook keyboardHook)
    {
        _preferencesRepository = preferencesRepository;
        _keyboardHook = keyboardHook;
        this.InitializeComponent();
        _ = LoadSettingsAsync();
    }

    private async System.Threading.Tasks.Task LoadSettingsAsync()
    {
        _isLoading = true;

        // Auto-start
        AutoStartToggle.IsOn = AutoStartManager.IsAutoStartEnabled;

        // Load preferences from repository
        var minimizeToTray = await _preferencesRepository.GetAsync("MinimizeToTray", CancellationToken.None);
        MinimizeToTrayToggle.IsOn = minimizeToTray?.Value == "true";

        var winKeyHook = await _preferencesRepository.GetAsync("WinKeyHookEnabled", CancellationToken.None);
        WinKeyHookToggle.IsOn = winKeyHook?.Value != "false"; // Default to true

        var themePref = await _preferencesRepository.GetAsync("Theme", CancellationToken.None);
        var theme = themePref?.Value ?? "System";
        foreach (ComboBoxItem item in ThemeComboBox.Items)
        {
            if (item.Tag?.ToString() == theme)
            {
                ThemeComboBox.SelectedItem = item;
                break;
            }
        }
        if (ThemeComboBox.SelectedItem is null)
            ThemeComboBox.SelectedIndex = 0;

        var showRecent = await _preferencesRepository.GetAsync("ShowRecentApps", CancellationToken.None);
        ShowRecentToggle.IsOn = showRecent?.Value != "false"; // Default to true

        _isLoading = false;
    }

    private void AutoStartToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        if (AutoStartToggle.IsOn)
            AutoStartManager.EnableAutoStart();
        else
            AutoStartManager.DisableAutoStart();
    }

    private async void MinimizeToTrayToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        await _preferencesRepository.UpsertAsync(
            new PreferenceInfo("MinimizeToTray", MinimizeToTrayToggle.IsOn.ToString().ToLower(), DateTimeOffset.UtcNow),
            CancellationToken.None);
    }

    private async void WinKeyHookToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;

        _keyboardHook.IsEnabled = WinKeyHookToggle.IsOn;
        await _preferencesRepository.UpsertAsync(
            new PreferenceInfo("WinKeyHookEnabled", WinKeyHookToggle.IsOn.ToString().ToLower(), DateTimeOffset.UtcNow),
            CancellationToken.None);

        HookWarningBar.IsOpen = WinKeyHookToggle.IsOn;
    }

    private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isLoading) return;

        if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag is string theme)
        {
            await _preferencesRepository.UpsertAsync(
                new PreferenceInfo("Theme", theme, DateTimeOffset.UtcNow),
                CancellationToken.None);

            // Apply theme
            if (App.Current.MainWindow is MainWindow mainWindow)
            {
                var requestedTheme = theme switch
                {
                    "Light" => ElementTheme.Light,
                    "Dark" => ElementTheme.Dark,
                    _ => ElementTheme.Default
                };

                if (mainWindow.Content is FrameworkElement root)
                {
                    root.RequestedTheme = requestedTheme;
                }
            }
        }
    }

    private async void ShowRecentToggle_Toggled(object sender, RoutedEventArgs e)
    {
        if (_isLoading) return;
        await _preferencesRepository.UpsertAsync(
            new PreferenceInfo("ShowRecentApps", ShowRecentToggle.IsOn.ToString().ToLower(), DateTimeOffset.UtcNow),
            CancellationToken.None);
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        BackRequested?.Invoke(this, EventArgs.Empty);
    }
}
