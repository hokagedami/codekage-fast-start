using FastStart.Core.Models;

namespace FastStart.Core.Services;

/// <summary>
/// Enumerates UWP applications.
/// </summary>
public interface IUwpAppEnumerator
{
    /// <summary>
    /// Enumerates UWP applications for indexing.
    /// </summary>
    IAsyncEnumerable<AppInfo> EnumerateAsync(CancellationToken ct);
}
