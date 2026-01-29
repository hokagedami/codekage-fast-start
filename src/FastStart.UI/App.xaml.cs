using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FastStart.Core;
using FastStart.Core.Services;
using FastStart.Data;
using FastStart.Native;
using FastStart.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Serilog;
using Serilog.Events;

namespace FastStart.UI;

public sealed partial class App : Application
{
    private readonly IHost _host;
    private readonly IStartupTiming _startupTiming;
    private readonly ILogger<App> _logger;
    private readonly CancellationTokenSource _appCts = new();
    private Window? _window;

    public App()
    {
        _startupTiming = new StartupTiming(Stopwatch.StartNew());
        _startupTiming.Mark(StartupMarker.ProcessStart);

        _host = CreateHostBuilder(Environment.GetCommandLineArgs(), _startupTiming).Build();
        _startupTiming.Mark(StartupMarker.HostBuilt);

        _logger = _host.Services.GetRequiredService<ILogger<App>>();
        InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = _host.Services.GetRequiredService<MainWindow>();
        _startupTiming.Mark(StartupMarker.WindowCreated);

        var themeService = _host.Services.GetRequiredService<ThemeService>();
        themeService.Initialize(_window);
        themeService.FollowSystem();

        _window.Activate();
        _startupTiming.Mark(StartupMarker.FirstWindowActivated);

        _window.Closed += OnWindowClosed;

        _ = Task.Run(async () =>
        {
            _startupTiming.Mark(StartupMarker.HostStarting);
            try
            {
                await _host.StartAsync(_appCts.Token).ConfigureAwait(false);
                _startupTiming.Mark(StartupMarker.HostStarted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Host start failed.");
            }
        });
    }

    private void OnWindowClosed(object sender, WindowEventArgs args)
    {
        _ = Task.Run(ShutdownAsync);
    }

    private async Task ShutdownAsync()
    {
        try
        {
            _appCts.Cancel();
            await _host.StopAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Host shutdown failed.");
        }
        finally
        {
            _host.Dispose();
        }
    }

    private static IHostBuilder CreateHostBuilder(string[] args, IStartupTiming startupTiming)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddSingleton(startupTiming);
                services.AddFastStartCore();
                services.AddFastStartData();
                services.AddFastStartNative();
                services.AddFastStartUI();
            })
            .UseSerilog((context, services, config) =>
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var logDir = Path.Combine(appData, "FastStart", "logs");
                Directory.CreateDirectory(logDir);

                config
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .WriteTo.File(
                        Path.Combine(logDir, "faststart-.log"),
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: 7,
                        shared: true);
            });
    }
}