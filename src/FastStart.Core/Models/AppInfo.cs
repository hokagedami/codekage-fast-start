namespace FastStart.Core.Models;

/// <summary>
/// Represents a launchable application entry.
/// </summary>
public sealed class AppInfo
{
    /// <summary>
    /// Gets or sets the database identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the executable path or package entry.
    /// </summary>
    public required string ExecutablePath { get; set; }

    /// <summary>
    /// Gets or sets optional arguments for launch.
    /// </summary>
    public string? Arguments { get; set; }

    /// <summary>
    /// Gets or sets the working directory.
    /// </summary>
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Gets or sets the icon path.
    /// </summary>
    public string? IconPath { get; set; }

    /// <summary>
    /// Gets or sets the source of the entry.
    /// </summary>
    public AppSource Source { get; set; }

    /// <summary>
    /// Gets or sets the UWP package family name if applicable.
    /// </summary>
    public string? PackageFamilyName { get; set; }

    /// <summary>
    /// Gets or sets the last indexed timestamp.
    /// </summary>
    public DateTimeOffset LastIndexedUtc { get; set; }
}
