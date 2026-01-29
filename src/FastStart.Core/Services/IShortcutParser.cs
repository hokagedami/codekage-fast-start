using FastStart.Core.Models;

namespace FastStart.Core.Services;

/// <summary>
/// Parses Windows shortcut files into application metadata.
/// </summary>
public interface IShortcutParser
{
    /// <summary>
    /// Parses a shortcut file into an application entry.
    /// </summary>
    Task<AppInfo?> ParseAsync(string shortcutPath, CancellationToken ct);
}
