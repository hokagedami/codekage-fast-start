namespace FastStart.Core.Models;

/// <summary>
/// Bundles application metadata with precomputed search tokens.
/// </summary>
public sealed class AppIndexEntry
{
    /// <summary>
    /// Gets or sets the application info.
    /// </summary>
    public required AppInfo App { get; set; }

    /// <summary>
    /// Gets or sets the search tokens.
    /// </summary>
    public required IReadOnlyList<string> Tokens { get; set; }
}
