using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Core.Services;
using Microsoft.Extensions.Logging;

namespace FastStart.Native;

public class AppLauncher : IAppLauncher
{
    private readonly IRecentLaunchRepository _recentLaunchRepository;
    private readonly ILogger<AppLauncher> _logger;

    public AppLauncher(IRecentLaunchRepository recentLaunchRepository, ILogger<AppLauncher> logger)
    {
        _recentLaunchRepository = recentLaunchRepository;
        _logger = logger;
    }

    public async Task<bool> LaunchAsync(AppInfo app, string? query, CancellationToken ct)
    {
        try
        {
            switch (app.Source)
            {
                case AppSource.Shortcut:
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = app.ExecutablePath,
                        UseShellExecute = true
                    };
                    if (!string.IsNullOrEmpty(app.Arguments))
                        startInfo.Arguments = app.Arguments;
                    if (!string.IsNullOrEmpty(app.WorkingDirectory))
                        startInfo.WorkingDirectory = app.WorkingDirectory;

                    Process.Start(startInfo);
                    break;

                case AppSource.Uwp:
                    var uri = new Uri($"shell:AppsFolder\\{app.ExecutablePath}");
                    await Windows.System.Launcher.LaunchUriAsync(uri);
                    break;
            }

            await _recentLaunchRepository.AddAsync(new RecentLaunchInfo
            {
                ApplicationId = app.Id,
                LaunchedAtUtc = DateTimeOffset.UtcNow,
                SearchQuery = query
            }, ct);
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Failed to launch app {AppName}", app.Name);
            return false;
        }
    }
}
