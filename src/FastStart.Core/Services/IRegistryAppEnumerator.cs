using FastStart.Core.Models;

namespace FastStart.Core.Services;

/// <summary>
/// Enumerates installed applications from Windows Registry.
/// </summary>
public interface IRegistryAppEnumerator
{
    /// <summary>
    /// Enumerates applications registered in the Windows Uninstall registry keys.
    /// </summary>
    IAsyncEnumerable<AppInfo> EnumerateAsync(CancellationToken ct);
}
