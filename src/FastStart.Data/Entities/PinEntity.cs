namespace FastStart.Data.Entities;

/// <summary>
/// Database entity for pinned apps.
/// </summary>
public sealed class PinEntity
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
    /// Gets or sets the pin position.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets the optional group name.
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Gets or sets the application navigation property.
    /// </summary>
    public AppEntity? Application { get; set; }
}
