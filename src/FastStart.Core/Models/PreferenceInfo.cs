namespace FastStart.Core.Models;

/// <summary>
/// Represents a persisted preference.
/// </summary>
public sealed class PreferenceInfo
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PreferenceInfo"/> class.
    /// </summary>
    public PreferenceInfo(string key, string value, DateTimeOffset updatedAtUtc)
    {
        Key = key;
        Value = value;
        UpdatedAtUtc = updatedAtUtc;
    }

    /// <summary>
    /// Gets the preference key.
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// Gets the preference value.
    /// </summary>
    public string Value { get; }

    /// <summary>
    /// Gets the last updated timestamp.
    /// </summary>
    public DateTimeOffset UpdatedAtUtc { get; }
}
