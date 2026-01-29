using FastStart.Core.Models;

namespace FastStart.Data.Entities;

/// <summary>
/// Database entity for applications.
/// </summary>
public sealed class AppEntity
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the executable path.
    /// </summary>
    public string ExecutablePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets optional arguments.
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
    /// Gets or sets the source.
    /// </summary>
    public AppSource Source { get; set; }

    /// <summary>
    /// Gets or sets the package family name.
    /// </summary>
    public string? PackageFamilyName { get; set; }

    /// <summary>
    /// Gets or sets the last indexed timestamp.
    /// </summary>
    public DateTimeOffset LastIndexedUtc { get; set; }

    /// <summary>
    /// Gets or sets related tokens.
    /// </summary>
    public ICollection<AppTokenEntity> Tokens { get; set; } = new List<AppTokenEntity>();
}
