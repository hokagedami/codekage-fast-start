namespace FastStart.Data.Entities;

/// <summary>
/// Database entity for application tokens.
/// </summary>
public sealed class AppTokenEntity
{
    /// <summary>
    /// Gets or sets the identifier.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Gets or sets the application identifier.
    /// </summary>
    public long AppId { get; set; }

    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the application.
    /// </summary>
    public AppEntity? App { get; set; }
}
