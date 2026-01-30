using System.Runtime.CompilerServices;
using FastStart.Core.Models;
using FastStart.Core.Services;

namespace FastStart.Native;

/// <summary>
/// Scans Program Files directories for executable applications.
/// </summary>
public sealed class ProgramFilesScanner : IProgramFilesScanner
{
    // Executables to skip (utilities, helpers, updaters, etc.)
    private static readonly HashSet<string> ExcludedFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "unins000.exe", "uninstall.exe", "uninst.exe",
        "update.exe", "updater.exe", "autoupdate.exe",
        "setup.exe", "install.exe", "installer.exe",
        "helper.exe", "crash_reporter.exe", "crashreporter.exe",
        "elevate.exe", "launcher.exe", "bootstrap.exe",
        "7z.exe", "7za.exe", "7zg.exe"
    };

    // Directory names to skip
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        "Common Files", "Windows Kits", "Windows NT", "Windows Defender",
        "Windows Mail", "Windows Media Player", "Windows Multimedia Platform",
        "Windows Photo Viewer", "Windows Portable Devices", "Windows Security",
        "Windows Sidebar", "WindowsPowerShell", "MSBuild", "Reference Assemblies",
        "Microsoft SDKs", "dotnet", "PackageManagement", "Uninstall Information",
        "Microsoft.NET", "Microsoft", "Microsoft Office", "Microsoft Update Health Tools"
    };

    // Known good applications that should be included even with suspicious names
    private static readonly HashSet<string> KnownGoodApps = new(StringComparer.OrdinalIgnoreCase)
    {
        "Code.exe", "devenv.exe", "rider64.exe", "idea64.exe", "pycharm64.exe",
        "chrome.exe", "firefox.exe", "msedge.exe", "brave.exe", "opera.exe",
        "slack.exe", "discord.exe", "teams.exe", "zoom.exe", "spotify.exe",
        "notepad++.exe", "sublime_text.exe", "atom.exe",
        "git-bash.exe", "bash.exe", "powershell.exe", "pwsh.exe",
        "winrar.exe", "7zFM.exe", "explorer++.exe"
    };

    /// <inheritdoc />
    public async IAsyncEnumerable<AppInfo> ScanAsync([EnumeratorCancellation] CancellationToken ct)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rootDir in GetProgramFilesDirectories())
        {
            if (ct.IsCancellationRequested)
                yield break;

            if (!Directory.Exists(rootDir))
                continue;

            await foreach (var app in ScanDirectoryAsync(rootDir, seen, ct).ConfigureAwait(false))
            {
                yield return app;
            }
        }
    }

    private static IEnumerable<string> GetProgramFilesDirectories()
    {
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        // Also scan LocalAppData for user-installed apps (Discord, Slack, etc.)
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        yield return Path.Combine(localAppData, "Programs");
    }

    private async IAsyncEnumerable<AppInfo> ScanDirectoryAsync(
        string rootDir,
        HashSet<string> seen,
        [EnumeratorCancellation] CancellationToken ct)
    {
        string[] appDirs;

        try
        {
            appDirs = await Task.Run(() => Directory.GetDirectories(rootDir), ct).ConfigureAwait(false);
        }
        catch
        {
            yield break;
        }

        foreach (var appDir in appDirs)
        {
            if (ct.IsCancellationRequested)
                yield break;

            var dirName = Path.GetFileName(appDir);

            // Skip excluded directories
            if (ExcludedDirectories.Contains(dirName))
                continue;

            // Skip hidden directories
            try
            {
                var attrs = File.GetAttributes(appDir);
                if ((attrs & FileAttributes.Hidden) != 0)
                    continue;
            }
            catch
            {
                continue;
            }

            await foreach (var app in FindExecutablesInAppDirAsync(appDir, dirName, seen, ct).ConfigureAwait(false))
            {
                yield return app;
            }
        }
    }

    private async IAsyncEnumerable<AppInfo> FindExecutablesInAppDirAsync(
        string appDir,
        string appName,
        HashSet<string> seen,
        [EnumeratorCancellation] CancellationToken ct)
    {
        string[] exeFiles;

        try
        {
            // Only scan first two levels to avoid going too deep
            exeFiles = await Task.Run(() =>
            {
                var files = new List<string>();

                // Root level
                files.AddRange(Directory.GetFiles(appDir, "*.exe", SearchOption.TopDirectoryOnly));

                // One level deep (for apps with bin folders, etc.)
                foreach (var subDir in Directory.GetDirectories(appDir))
                {
                    try
                    {
                        var subDirName = Path.GetFileName(subDir).ToLowerInvariant();
                        // Only scan likely app directories
                        if (subDirName is "bin" or "app" or "current" or "application")
                        {
                            files.AddRange(Directory.GetFiles(subDir, "*.exe", SearchOption.TopDirectoryOnly));
                        }
                    }
                    catch
                    {
                        // Skip inaccessible directories
                    }
                }

                return files.ToArray();
            }, ct).ConfigureAwait(false);
        }
        catch
        {
            yield break;
        }

        // Find the "main" executable for this app
        var mainExe = SelectMainExecutable(exeFiles, appName);

        if (mainExe is not null && seen.Add(mainExe))
        {
            var fileName = Path.GetFileNameWithoutExtension(mainExe);
            var displayName = GetDisplayName(fileName, appName);

            yield return new AppInfo
            {
                Name = displayName,
                ExecutablePath = mainExe,
                IconPath = mainExe,
                Source = AppSource.ProgramFiles,
                WorkingDirectory = Path.GetDirectoryName(mainExe)
            };
        }
    }

    private static string? SelectMainExecutable(string[] exeFiles, string appDirName)
    {
        if (exeFiles.Length == 0)
            return null;

        var candidates = exeFiles
            .Where(f => !IsExcludedExecutable(Path.GetFileName(f)))
            .ToList();

        if (candidates.Count == 0)
            return null;

        if (candidates.Count == 1)
            return candidates[0];

        // Prefer known good apps
        var knownGood = candidates.FirstOrDefault(f => KnownGoodApps.Contains(Path.GetFileName(f)));
        if (knownGood is not null)
            return knownGood;

        // Prefer executable that matches the directory name
        var dirMatch = candidates.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f).Equals(appDirName, StringComparison.OrdinalIgnoreCase));
        if (dirMatch is not null)
            return dirMatch;

        // Prefer executable that contains the directory name
        var containsMatch = candidates.FirstOrDefault(f =>
            Path.GetFileNameWithoutExtension(f).Contains(appDirName, StringComparison.OrdinalIgnoreCase));
        if (containsMatch is not null)
            return containsMatch;

        // Prefer largest executable (likely the main app)
        try
        {
            return candidates
                .Select(f => new { Path = f, Size = new FileInfo(f).Length })
                .OrderByDescending(x => x.Size)
                .First()
                .Path;
        }
        catch
        {
            return candidates.First();
        }
    }

    private static bool IsExcludedExecutable(string fileName)
    {
        if (ExcludedFileNames.Contains(fileName))
            return true;

        var lower = fileName.ToLowerInvariant();

        // Skip various utility executables by pattern
        if (lower.StartsWith("unins") ||
            lower.Contains("update") ||
            lower.Contains("setup") ||
            lower.Contains("install") ||
            lower.Contains("crash") ||
            lower.Contains("helper") ||
            lower.Contains("report") ||
            lower.EndsWith("_helper.exe") ||
            lower.EndsWith("updater.exe") ||
            lower.EndsWith("service.exe"))
        {
            // Unless it's a known good app
            return !KnownGoodApps.Contains(fileName);
        }

        return false;
    }

    private static string GetDisplayName(string exeName, string dirName)
    {
        // If the exe name is meaningful, use it
        if (!string.IsNullOrWhiteSpace(exeName) &&
            !exeName.Equals("app", StringComparison.OrdinalIgnoreCase) &&
            !exeName.Equals("main", StringComparison.OrdinalIgnoreCase) &&
            !exeName.Equals("run", StringComparison.OrdinalIgnoreCase))
        {
            return FormatDisplayName(exeName);
        }

        return FormatDisplayName(dirName);
    }

    private static string FormatDisplayName(string name)
    {
        // Convert common patterns to readable names
        // "my-app" -> "My App"
        // "myApp" -> "My App" (basic camel case handling)
        // "my_app" -> "My App"

        var result = name
            .Replace('-', ' ')
            .Replace('_', ' ')
            .Replace('.', ' ');

        // Basic title case
        if (result.Length > 0)
        {
            var chars = result.ToCharArray();
            chars[0] = char.ToUpper(chars[0]);
            for (int i = 1; i < chars.Length; i++)
            {
                if (chars[i - 1] == ' ')
                    chars[i] = char.ToUpper(chars[i]);
            }
            result = new string(chars);
        }

        return result.Trim();
    }
}
