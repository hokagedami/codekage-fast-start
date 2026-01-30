using FastStart.Core.Models;

namespace FastStart.Core.Services;

/// <summary>
/// Scans Program Files directories for executable applications.
/// </summary>
public interface IProgramFilesScanner
{
    /// <summary>
    /// Scans Program Files directories for executables.
    /// </summary>
    IAsyncEnumerable<AppInfo> ScanAsync(CancellationToken ct);
}
