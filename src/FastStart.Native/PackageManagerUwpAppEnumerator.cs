using System.Runtime.CompilerServices;
using FastStart.Core.Services;
using Microsoft.Extensions.Logging;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using AppInfo = FastStart.Core.Models.AppInfo;
using AppSource = FastStart.Core.Models.AppSource;

namespace FastStart.Native;

/// <summary>
/// Enumerates UWP applications using the Windows PackageManager API.
/// </summary>
public sealed class PackageManagerUwpAppEnumerator : IUwpAppEnumerator
{
    private readonly ILogger<PackageManagerUwpAppEnumerator> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageManagerUwpAppEnumerator"/> class.
    /// </summary>
    public PackageManagerUwpAppEnumerator(ILogger<PackageManagerUwpAppEnumerator> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AppInfo> EnumerateAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var packageManager = new PackageManager();
        IEnumerable<Package> packages;

        try
        {
            // Get all packages for the current user
            packages = packageManager.FindPackagesForUser(string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to enumerate packages");
            yield break;
        }

        foreach (var package in packages)
        {
            if (ct.IsCancellationRequested)
            {
                yield break;
            }

            // Skip framework and resource packages - they're not launchable apps
            if (package.IsFramework || package.IsResourcePackage)
            {
                continue;
            }

            AppInfo? appInfo = null;
            try
            {
                appInfo = await ExtractAppInfoAsync(package).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to extract info for package: {PackageName}", package.Id?.FullName);
            }

            if (appInfo is not null)
            {
                yield return appInfo;
            }
        }
    }

    private async Task<AppInfo?> ExtractAppInfoAsync(Package package)
    {
        // Get the package manifest to find the app entry point
        var apps = await Task.Run(() =>
        {
            try
            {
                return package.GetAppListEntries();
            }
            catch
            {
                return null;
            }
        }).ConfigureAwait(false);

        if (apps is null || apps.Count == 0)
        {
            return null;
        }

        // Use the first app entry (most packages have only one)
        var app = apps[0];
        var displayName = app.DisplayInfo?.DisplayName;

        if (string.IsNullOrWhiteSpace(displayName))
        {
            return null;
        }

        // Build the launch URI using the Application User Model ID (AUMID)
        var appUserModelId = app.AppUserModelId;
        if (string.IsNullOrWhiteSpace(appUserModelId))
        {
            return null;
        }

        // Get the logo path - use the package installation path to locate the logo
        string? iconPath = null;
        try
        {
            var installedPath = package.InstalledPath;
            if (!string.IsNullOrEmpty(installedPath))
            {
                // UWP app logos are typically stored in Assets folder
                // The AUMID can be used as a reference for shell icon resolution
                iconPath = $"shell:AppsFolder\\{appUserModelId}";
            }
        }
        catch
        {
            // Icon extraction can fail for various reasons, continue without icon
        }

        return new AppInfo
        {
            Name = displayName,
            ExecutablePath = appUserModelId,
            IconPath = iconPath,
            PackageFamilyName = package.Id?.FamilyName,
            Source = AppSource.Uwp,
            LastIndexedUtc = DateTimeOffset.UtcNow
        };
    }
}
