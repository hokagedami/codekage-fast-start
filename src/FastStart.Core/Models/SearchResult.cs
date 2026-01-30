namespace FastStart.Core.Models;

/// <summary>
/// Represents a scored search result.
/// </summary>
public readonly struct SearchResult
{
    /// <summary>
    /// Gets the matched application.
    /// </summary>
    public required AppInfo App { get; init; }

    /// <summary>
    /// Gets the computed score.
    /// </summary>
    public required int Score { get; init; }

    /// <summary>
    /// Gets the match kind.
    /// </summary>
    public required SearchMatchKind MatchKind { get; init; }
}
