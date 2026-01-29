namespace FastStart.Core.Models;

/// <summary>
/// Represents a scored search result.
/// </summary>
public sealed class SearchResult
{
    /// <summary>
    /// Gets or sets the matched application.
    /// </summary>
    public required AppInfo App { get; set; }

    /// <summary>
    /// Gets or sets the computed score.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Gets or sets the match kind.
    /// </summary>
    public SearchMatchKind MatchKind { get; set; }
}
