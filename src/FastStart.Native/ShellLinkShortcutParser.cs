using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using FastStart.Core.Models;
using FastStart.Core.Services;
using FastStart.Native.Com;
using Microsoft.Extensions.Logging;

namespace FastStart.Native;

/// <summary>
/// Parses Windows shortcut (.lnk) files using IShellLink COM interface.
/// </summary>
public sealed class ShellLinkShortcutParser : IShortcutParser
{
    private const int MaxPath = 260;
    private readonly ILogger<ShellLinkShortcutParser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShellLinkShortcutParser"/> class.
    /// </summary>
    public ShellLinkShortcutParser(ILogger<ShellLinkShortcutParser> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task<AppInfo?> ParseAsync(string shortcutPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(shortcutPath) || !File.Exists(shortcutPath))
        {
            return Task.FromResult<AppInfo?>(null);
        }

        // Run COM operations on a thread pool thread to avoid blocking
        return Task.Run(() => ParseShortcut(shortcutPath), ct);
    }

    private AppInfo? ParseShortcut(string shortcutPath)
    {
        IShellLinkW? shellLink = null;
        IPersistFile? persistFile = null;

        try
        {
            // Create ShellLink COM object
            var shellLinkType = Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046"));
            if (shellLinkType is null)
            {
                _logger.LogWarning("Failed to get ShellLink type from CLSID");
                return null;
            }

            var shellLinkObj = Activator.CreateInstance(shellLinkType);
            if (shellLinkObj is null)
            {
                _logger.LogWarning("Failed to create ShellLink instance");
                return null;
            }

            shellLink = (IShellLinkW)shellLinkObj;
            persistFile = (IPersistFile)shellLinkObj;

            // Load the shortcut file
            persistFile.Load(shortcutPath, 0); // STGM_READ = 0

            // Get target path
            var pathBuilder = new StringBuilder(MaxPath);
            shellLink.GetPath(pathBuilder, MaxPath, IntPtr.Zero, ShellLinkPathFlags.SLGP_RAWPATH);
            var targetPath = pathBuilder.ToString();

            // Skip shortcuts that don't have a valid target
            if (string.IsNullOrWhiteSpace(targetPath))
            {
                return null;
            }

            // Skip URLs and non-executable targets
            if (targetPath.StartsWith("http", StringComparison.OrdinalIgnoreCase) ||
                targetPath.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            // Get display name from shortcut filename
            var name = Path.GetFileNameWithoutExtension(shortcutPath);

            // Get arguments
            var argsBuilder = new StringBuilder(MaxPath * 4);
            shellLink.GetArguments(argsBuilder, argsBuilder.Capacity);
            var arguments = argsBuilder.ToString();

            // Get working directory
            var workDirBuilder = new StringBuilder(MaxPath);
            shellLink.GetWorkingDirectory(workDirBuilder, MaxPath);
            var workingDirectory = workDirBuilder.ToString();

            // Get icon location
            var iconBuilder = new StringBuilder(MaxPath);
            shellLink.GetIconLocation(iconBuilder, MaxPath, out var iconIndex);
            var iconPath = iconBuilder.ToString();

            // If no explicit icon, use the target executable
            if (string.IsNullOrWhiteSpace(iconPath) && File.Exists(targetPath))
            {
                iconPath = targetPath;
                iconIndex = 0;
            }

            return new AppInfo
            {
                Name = name,
                ExecutablePath = targetPath,
                Arguments = string.IsNullOrWhiteSpace(arguments) ? null : arguments,
                WorkingDirectory = string.IsNullOrWhiteSpace(workingDirectory) ? null : workingDirectory,
                IconPath = string.IsNullOrWhiteSpace(iconPath) ? null : FormatIconPath(iconPath, iconIndex),
                Source = AppSource.Shortcut,
                LastIndexedUtc = DateTimeOffset.UtcNow
            };
        }
        catch (COMException ex)
        {
            _logger.LogDebug(ex, "COM error parsing shortcut: {Path}", shortcutPath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parsing shortcut: {Path}", shortcutPath);
            return null;
        }
        finally
        {
            // Release COM objects
            if (persistFile is not null)
            {
                Marshal.ReleaseComObject(persistFile);
            }

            if (shellLink is not null)
            {
                Marshal.ReleaseComObject(shellLink);
            }
        }
    }

    private static string FormatIconPath(string iconPath, int iconIndex)
    {
        if (iconIndex == 0)
        {
            return iconPath;
        }

        return $"{iconPath},{iconIndex}";
    }
}
