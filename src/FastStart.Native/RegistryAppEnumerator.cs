using System.Runtime.CompilerServices;
using FastStart.Core.Models;
using FastStart.Core.Services;
using Microsoft.Win32;

namespace FastStart.Native;

/// <summary>
/// Enumerates installed applications from Windows Registry uninstall keys.
/// </summary>
public sealed class RegistryAppEnumerator : IRegistryAppEnumerator
{
    private static readonly string[] UninstallKeyPaths =
    [
        @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall",
        @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall"
    ];

    /// <inheritdoc />
    public async IAsyncEnumerable<AppInfo> EnumerateAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Scan both HKLM and HKCU for installed apps
        foreach (var (hive, hiveName) in new[] { (Registry.LocalMachine, "HKLM"), (Registry.CurrentUser, "HKCU") })
        {
            foreach (var keyPath in UninstallKeyPaths)
            {
                if (ct.IsCancellationRequested)
                    yield break;

                await foreach (var app in ScanRegistryKeyAsync(hive, keyPath, seen, ct).ConfigureAwait(false))
                {
                    yield return app;
                }
            }
        }
    }

    private static async IAsyncEnumerable<AppInfo> ScanRegistryKeyAsync(
        RegistryKey hive,
        string keyPath,
        HashSet<string> seen,
        [EnumeratorCancellation] CancellationToken ct)
    {
        RegistryKey? uninstallKey = null;
        string[]? subKeyNames = null;

        try
        {
            uninstallKey = hive.OpenSubKey(keyPath);
            if (uninstallKey is null)
                yield break;

            subKeyNames = await Task.Run(() => uninstallKey.GetSubKeyNames(), ct).ConfigureAwait(false);
        }
        catch
        {
            yield break;
        }

        foreach (var subKeyName in subKeyNames)
        {
            if (ct.IsCancellationRequested)
                yield break;

            AppInfo? app = null;

            try
            {
                using var subKey = uninstallKey.OpenSubKey(subKeyName);
                if (subKey is null)
                    continue;

                // Skip system components and updates
                var systemComponent = subKey.GetValue("SystemComponent");
                if (systemComponent is int sc && sc == 1)
                    continue;

                var parentKeyName = subKey.GetValue("ParentKeyName") as string;
                if (!string.IsNullOrEmpty(parentKeyName))
                    continue; // This is an update/patch, skip it

                var displayName = subKey.GetValue("DisplayName") as string;
                if (string.IsNullOrWhiteSpace(displayName))
                    continue;

                // Get the executable path - try multiple registry values
                var executablePath = GetExecutablePath(subKey);
                if (string.IsNullOrWhiteSpace(executablePath))
                    continue;

                // Skip if we've already seen this executable
                if (!seen.Add(executablePath))
                    continue;

                // Skip if the file doesn't exist
                if (!File.Exists(executablePath))
                    continue;

                var iconPath = subKey.GetValue("DisplayIcon") as string;
                if (!string.IsNullOrEmpty(iconPath))
                {
                    // Icon path might have ",0" suffix for icon index - extract just the path
                    var commaIndex = iconPath.LastIndexOf(',');
                    if (commaIndex > 0)
                        iconPath = iconPath[..commaIndex].Trim('"');
                }

                app = new AppInfo
                {
                    Name = CleanDisplayName(displayName),
                    ExecutablePath = executablePath,
                    IconPath = iconPath,
                    Source = AppSource.Registry,
                    WorkingDirectory = Path.GetDirectoryName(executablePath)
                };
            }
            catch
            {
                // Skip entries that cause errors
                continue;
            }

            if (app is not null)
                yield return app;
        }

        uninstallKey?.Dispose();
    }

    private static string? GetExecutablePath(RegistryKey subKey)
    {
        // Try DisplayIcon first (often points to main executable)
        var displayIcon = subKey.GetValue("DisplayIcon") as string;
        if (!string.IsNullOrEmpty(displayIcon))
        {
            var iconPath = ExtractPathFromIconString(displayIcon);
            if (IsValidExecutable(iconPath))
                return iconPath;
        }

        // Try InstallLocation + look for main executable
        var installLocation = subKey.GetValue("InstallLocation") as string;
        if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
        {
            var exe = FindMainExecutable(installLocation);
            if (exe is not null)
                return exe;
        }

        return null;
    }

    private static string ExtractPathFromIconString(string iconString)
    {
        // Remove quotes and icon index
        var path = iconString.Trim('"');
        var commaIndex = path.LastIndexOf(',');
        if (commaIndex > 0)
            path = path[..commaIndex].Trim();

        return path;
    }

    private static bool IsValidExecutable(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(path);
    }

    private static string? FindMainExecutable(string directory)
    {
        try
        {
            // Look for executables in the root directory first
            var exes = Directory.GetFiles(directory, "*.exe", SearchOption.TopDirectoryOnly);

            // Prefer executables that don't look like uninstallers or updaters
            var validExes = exes
                .Where(e => !IsUtilityExecutable(Path.GetFileName(e)))
                .ToArray();

            if (validExes.Length == 1)
                return validExes[0];

            // If multiple, try to find one that matches the directory name
            var dirName = Path.GetFileName(directory);
            var match = validExes.FirstOrDefault(e =>
                Path.GetFileNameWithoutExtension(e).Contains(dirName, StringComparison.OrdinalIgnoreCase));

            return match ?? validExes.FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    private static bool IsUtilityExecutable(string fileName)
    {
        var lower = fileName.ToLowerInvariant();
        return lower.Contains("unins") ||
               lower.Contains("update") ||
               lower.Contains("setup") ||
               lower.Contains("install") ||
               lower.Contains("helper") ||
               lower.Contains("crash") ||
               lower.Contains("reporter") ||
               lower.StartsWith("ui") && lower.Length < 10;
    }

    private static string CleanDisplayName(string name)
    {
        // Remove version numbers from the end
        var cleaned = name.Trim();

        // Remove common suffixes
        foreach (var suffix in new[] { " (x64)", " (x86)", " (64-bit)", " (32-bit)" })
        {
            if (cleaned.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned[..^suffix.Length].Trim();
        }

        return cleaned;
    }
}
