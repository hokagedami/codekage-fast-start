namespace FastStart.Data.Entities;

/// <summary>
/// Tracks database schema versioning.
/// </summary>
public sealed class SchemaVersionEntity
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the schema version.
    /// </summary>
    public int Version { get; set; }

    /// <summary>
    /// Gets or sets when the schema was last updated.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
