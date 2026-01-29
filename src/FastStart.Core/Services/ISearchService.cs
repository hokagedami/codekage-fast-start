using FastStart.Core.Models;

namespace FastStart.Core.Services;

/// <summary>
/// Executes application searches.
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// Searches indexed applications using the supplied query.
    /// </summary>
    Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct);
}
