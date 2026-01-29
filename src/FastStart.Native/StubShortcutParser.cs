using FastStart.Core.Models;
using FastStart.Core.Services;

namespace FastStart.Native;

/// <summary>
/// Placeholder shortcut parser until COM interop is implemented.
/// </summary>
public sealed class StubShortcutParser : IShortcutParser
{
    /// <inheritdoc />
    public Task<AppInfo?> ParseAsync(string shortcutPath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(shortcutPath))
        {
            return Task.FromResult<AppInfo?>(null);
        }

        var name = Path.GetFileNameWithoutExtension(shortcutPath);
        var app = new AppInfo
        {
            Name = name,
            ExecutablePath = shortcutPath,
            Source = AppSource.Shortcut,
            LastIndexedUtc = DateTimeOffset.UtcNow
        };

        return Task.FromResult<AppInfo?>(app);
    }
}
