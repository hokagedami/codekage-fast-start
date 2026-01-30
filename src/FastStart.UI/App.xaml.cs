using System.Diagnostics;
using FastStart.Core;
using FastStart.Core.Repositories;
using FastStart.Core.Services;
using FastStart.Data;
using FastStart.Native;
using FastStart.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace FastStart.UI;

public sealed partial class App : Application
{
    private readonly ServiceProvider _services;
    private readonly IStartupTiming _startupTiming;
    private readonly ILogger<App> _logger;
    private readonly CancellationTokenSource _appCts = new();
    private readonly GlobalKeyboardHook _keyboardHook;
    private readonly TrayIconManager _trayIcon;
    private readonly bool _startMinimized;
    private readonly bool _diagnosticMode;
    private MainWindow? _window;
    private bool _minimizeToTray;

    public new static App Current => (App)Application.Current;
    public MainWindow? MainWindow => _window;
    public IServiceProvider Services => _services;

    public App()
    {
        _startupTiming = new StartupTiming(Stopwatch.StartNew());
        _startupTiming.Mark(StartupMarker.ProcessStart);

        // Check for command line arguments
        var args = Environment.GetCommandLineArgs();
        _startMinimized = args.Contains("--background");
        _diagnosticMode = args.Contains("--diag");

        // Use minimal DI without generic host overhead
        _services = CreateMinimalServiceProvider(_startupTiming);
        _startupTiming.Mark(StartupMarker.HostBuilt);

        _logger = _services.GetRequiredService<ILogger<App>>();
        _keyboardHook = _services.GetRequiredService<GlobalKeyboardHook>();
        _trayIcon = new TrayIconManager();

        InitializeComponent();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        try
        {
            // Create and show window immediately - don't block on anything
            _startupTiming.Mark(StartupMarker.WindowResolving);
            _window = _services.GetRequiredService<MainWindow>();
            _startupTiming.Mark(StartupMarker.WindowCreated);

            _window.Closed += OnWindowClosed;

            // Show window immediately (empty state is fine)
            if (!_startMinimized)
            {
                _window.Activate();
                _startupTiming.Mark(StartupMarker.FirstWindowActivated);
                LogStartupDiagnostics();
            }

            // Initialize everything else in background after window is visible
            _ = InitializeAfterWindowShownAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Application launch failed.");
            await ShutdownAsync();
        }
    }

    private async Task InitializeAfterWindowShownAsync()
    {
        try
        {
            // Apply theme
            var themeService = _services.GetRequiredService<ThemeService>();
            themeService.Initialize(_window!);
            themeService.FollowSystem();

            // Load preferences and setup features
            var prefsRepo = _services.GetRequiredService<IPreferencesRepository>();
            var minimizeToTrayPref = await prefsRepo.GetAsync("MinimizeToTray", _appCts.Token);
            _minimizeToTray = minimizeToTrayPref?.Value == "true";
            var hookEnabledPref = await prefsRepo.GetAsync("WinKeyHookEnabled", _appCts.Token);
            var hookEnabled = hookEnabledPref?.Value != "false";

            // Setup keyboard hook
            _keyboardHook.IsEnabled = hookEnabled;
            _keyboardHook.WinKeyPressed += OnWinKeyPressed;
            _keyboardHook.Start();

            // Setup tray icon
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(_window!);
            _trayIcon.WindowHandle = hWnd;
            if (_minimizeToTray)
            {
                _trayIcon.Show();
            }

            // Initialize database and start indexing in background
            _startupTiming.Mark(StartupMarker.HostStarting);
            var dbInitializer = _services.GetRequiredService<IDatabaseInitializer>();
            await dbInitializer.InitializeAsync(_appCts.Token).ConfigureAwait(false);

            // Start app indexing
            var indexService = _services.GetRequiredService<AppIndexService>();
            _ = indexService.StartAsync(_appCts.Token);
            _startupTiming.Mark(StartupMarker.HostStarted);

            // Log memory after indexing completes
            await Task.Delay(2000, _appCts.Token);
            LogMemoryDiagnostics();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Background initialization failed.");
        }
    }

    private void OnWinKeyPressed(object? sender, EventArgs e)
    {
        if (_window is null) return;

        _window.DispatcherQueue.TryEnqueue(() =>
        {
            _window.ToggleVisibility();
        });
    }

    private async void OnWindowClosed(object sender, WindowEventArgs args)
    {
        try
        {
            // If minimize to tray is enabled, hide instead of close
            if (_minimizeToTray)
            {
                args.Handled = true;
                _window?.HideWindow();
                _trayIcon.Show();
                return;
            }

            await ShutdownAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error during window close.");
            await ShutdownAsync();
        }
    }

    private async Task ShutdownAsync()
    {
        try
        {
            _keyboardHook.Stop();
            _keyboardHook.Dispose();
            _trayIcon.Dispose();
            _appCts.Cancel();

            // Stop indexing service
            var indexService = _services.GetRequiredService<AppIndexService>();
            await indexService.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shutdown failed.");
        }
        finally
        {
            await _services.DisposeAsync();
        }
    }

    public async Task ExitApplicationAsync()
    {
        _minimizeToTray = false; // Prevent minimize on close
        await ShutdownAsync();
        Environment.Exit(0);
    }

    private void LogStartupDiagnostics()
    {
        if (_startupTiming is StartupTiming timing)
        {
            var report = timing.GenerateReport();
            _logger.LogInformation("{StartupReport}", report);

            if (_diagnosticMode)
            {
                Console.WriteLine(report);
                WriteDiagnosticsToFile("startup", report);
            }

            System.Diagnostics.Debug.WriteLine(report);
        }
    }

    private void LogMemoryDiagnostics()
    {
        var memoryStats = StartupTiming.GetMemoryStats();
        var report = memoryStats.GenerateReport();
        _logger.LogInformation("{MemoryReport}", report);

        if (_diagnosticMode)
        {
            Console.WriteLine(report);
            WriteDiagnosticsToFile("memory", report);
            Console.WriteLine("\nDiagnostic mode complete. Exiting...");
            _ = ExitApplicationAsync();
        }

        System.Diagnostics.Debug.WriteLine(report);
    }

    private static void WriteDiagnosticsToFile(string name, string content)
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var diagDir = Path.Combine(appData, "FastStart", "diagnostics");
            Directory.CreateDirectory(diagDir);
            var filePath = Path.Combine(diagDir, $"{name}-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
            File.WriteAllText(filePath, content);
            Console.WriteLine($"Saved to: {filePath}");
        }
        catch
        {
            // Ignore file write errors in diagnostics
        }
    }

    private static ServiceProvider CreateMinimalServiceProvider(IStartupTiming startupTiming)
    {
        var services = new ServiceCollection();

        // Setup deferred Serilog (lazy file sink initialization)
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var logDir = Path.Combine(appData, "FastStart", "logs");

        var loggerConfig = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
            .Enrich.FromLogContext();

        // Only add file sink - defer directory creation
        _ = Task.Run(() =>
        {
            Directory.CreateDirectory(logDir);
        });

        loggerConfig.WriteTo.File(
            Path.Combine(logDir, "faststart-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            shared: true);

        Log.Logger = loggerConfig.CreateLogger();

        // Add logging
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog(Log.Logger, dispose: true);
        });

        // Register core services
        services.AddSingleton(startupTiming);
        services.AddSingleton<GlobalKeyboardHook>();
        services.AddFastStartCore();
        services.AddFastStartData();
        services.AddFastStartNative();
        services.AddFastStartUI();

        return services.BuildServiceProvider();
    }
}
