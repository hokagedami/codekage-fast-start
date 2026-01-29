namespace FastStart.Data.Entities;

/// <summary>
/// Database entity for recent launches.
/// </summary>
public sealed class RecentLaunchEntity
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    public long ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the launch timestamp.
    /// </summary>
    public DateTimeOffset LaunchedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    public string? SearchQuery { get; set; }

    /// <summary>
    /// Gets or sets the application navigation property.
    /// </summary>
    public AppEntity? Application { get; set; }
}
