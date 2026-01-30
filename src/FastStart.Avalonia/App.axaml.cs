using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FastStart.Avalonia.Services;
using FastStart.Avalonia.ViewModels;
using FastStart.Core;
using FastStart.Core.Repositories;
using FastStart.Core.Services;
using FastStart.Data;
using FastStart.Native;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace FastStart.Avalonia;

public partial class App : Application
{
    private ServiceProvider? _services;
    private MainWindow? _mainWindow;
    private GlobalKeyboardHook? _keyboardHook;
    private TrayIconManager? _trayIcon;
    private readonly CancellationTokenSource _appCts = new();
    private bool _minimizeToTray;
    private ILogger<App>? _logger;

    public static new App? Current => Application.Current as App;
    public IServiceProvider? Services => _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        _services = CreateServiceProvider();
        Program.StartupTiming.Mark(StartupMarker.HostBuilt);

        _logger = _services.GetRequiredService<ILogger<App>>();
        _keyboardHook = _services.GetRequiredService<GlobalKeyboardHook>();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            Program.StartupTiming.Mark(StartupMarker.WindowResolving);
            _mainWindow = _services.GetRequiredService<MainWindow>();
            Program.StartupTiming.Mark(StartupMarker.WindowCreated);

            desktop.MainWindow = _mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            if (!Program.StartMinimized)
            {
                _mainWindow.Show();
                Program.StartupTiming.Mark(StartupMarker.FirstWindowActivated);
                LogStartupDiagnostics();
            }

            _ = InitializeAfterWindowShownAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async Task InitializeAfterWindowShownAsync()
    {
        try
        {
            // Load preferences
            var prefsRepo = _services!.GetRequiredService<IPreferencesRepository>();
            var minimizeToTrayPref = await prefsRepo.GetAsync("MinimizeToTray", _appCts.Token);
            _minimizeToTray = minimizeToTrayPref?.Value == "true";
            var hookEnabledPref = await prefsRepo.GetAsync("WinKeyHookEnabled", _appCts.Token);
            var hookEnabled = hookEnabledPref?.Value != "false";

            // Setup keyboard hook
            _keyboardHook!.IsEnabled = hookEnabled;
            _keyboardHook.WinKeyPressed += OnWinKeyPressed;
            _keyboardHook.Start();

            // Setup tray icon
            _trayIcon = new TrayIconManager();
            if (_minimizeToTray)
            {
                _trayIcon.Show();
            }

            // Initialize database and start indexing
            Program.StartupTiming.Mark(StartupMarker.HostStarting);
            var dbInitializer = _services.GetRequiredService<IDatabaseInitializer>();
            await dbInitializer.InitializeAsync(_appCts.Token).ConfigureAwait(false);

            var indexService = _services.GetRequiredService<AppIndexService>();
            _ = indexService.StartAsync(_appCts.Token);
            Program.StartupTiming.Mark(StartupMarker.HostStarted);

            // Log memory after indexing
            await Task.Delay(2000, _appCts.Token);
            LogMemoryDiagnostics();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger?.LogError(ex, "Background initialization failed.");
        }
    }

    private void OnWinKeyPressed(object? sender, EventArgs e)
    {
        if (_mainWindow is null) return;

        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            _mainWindow.ToggleVisibility();
        });
    }

    private void LogStartupDiagnostics()
    {
        if (Program.StartupTiming is StartupTiming timing)
        {
            var report = timing.GenerateReport();
            _logger?.LogInformation("{StartupReport}", report);

            if (Program.DiagnosticMode)
            {
                Console.WriteLine(report);
                WriteDiagnosticsToFile("startup", report);
            }
        }
    }

    private void LogMemoryDiagnostics()
    {
        var memoryStats = StartupTiming.GetMemoryStats();
        var report = memoryStats.GenerateReport();
        _logger?.LogInformation("{MemoryReport}", report);

        if (Program.DiagnosticMode)
        {
            Console.WriteLine(report);
            WriteDiagnosticsToFile("memory", report);
            Console.WriteLine("\nDiagnostic mode complete. Exiting...");
            ExitApplication();
        }
    }

    private static void WriteDiagnosticsToFile(string name, string content)
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var diagDir = Path.Combine(appData, "FastStart", "diagnostics");
            Directory.CreateDirectory(diagDir);
            var filePath = Path.Combine(diagDir, $"avalonia-{name}-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"Saved to: {filePath}");
        }
        catch
        {
            // Ignore file write errors
        }
    }

    public void ExitApplication()
    {
        _appCts.Cancel();
        _keyboardHook?.Stop();
        _keyboardHook?.Dispose();
        _trayIcon?.Dispose();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private static ServiceProvider CreateServiceProvider()
    {
        var services = new ServiceCollection();

        // Setup Serilog
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "FastStart", "logs");
        Directory.CreateDirectory(logDir);

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext()
            .WriteTo.File(
                Path.Combine(logDir, "faststart-avalonia-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                shared: true);

        Log.Logger = loggerConfig.CreateLogger();

        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        // Register services
        services.AddSingleton(Program.StartupTiming);
        services.AddSingleton<GlobalKeyboardHook>();
        services.AddFastStartCore();
        services.AddFastStartData();
        services.AddFastStartNative();

        // Avalonia-specific services
        services.AddSingleton<IconService>();
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider();
    }
}
