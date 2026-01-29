namespace FastStart.Data.Entities;

/// <summary>
/// Database entity for preferences.
/// </summary>
public sealed class PreferenceEntity
{
    /// <summary>
    /// Gets or sets the key.
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the updated timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
