namespace FastStart.Core.Models;

/// <summary>
/// Represents a pinned application entry.
/// </summary>
public sealed class PinInfo
{
    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    public long ApplicationId { get; set; }

    /// <summary>
    /// Gets or sets the pin order position.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets an optional group name.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets the application info (for display).
    /// </summary>
    public AppInfo? App { get; set; }

    /// <summary>
    /// Alias for Position for UI compatibility.
    /// </summary>
    public int Order => Position;
}
