namespace FastStart.Core.Models;

/// <summary>
/// Represents a recent application launch record.
/// </summary>
public sealed class RecentLaunchInfo
{
    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    public long ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the launch timestamp.
    /// </summary>
    public DateTimeOffset LaunchedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the query text that triggered the launch.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the application info (for display).
    /// </summary>
    public AppInfo? App { get; set; }
}
