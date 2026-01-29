using FastStart.Core.Models;
using FastStart.Core.Repositories;
using Microsoft.Extensions.Logging;

namespace FastStart.Core.Services;

/// <summary>
/// Default search service implementation.
/// </summary>
public sealed class SearchService : ISearchService
{
    private const int MaxResults = 20;
    private const int TokenPrefixThreshold = 3; // Use token prefix filtering for queries <= 3 chars
    private readonly IAppRepository _appRepository;
    private readonly FuzzyScorer _scorer;
    private readonly ILogger<SearchService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SearchService"/> class.
    /// </summary>
    public SearchService(IAppRepository appRepository, FuzzyScorer scorer, ILogger<SearchService> logger)
    {
        _appRepository = appRepository;
        _scorer = scorer;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<SearchResult>();
        }

        var normalized = query.Trim();

        // For short queries, use token-based prefix filtering for faster results
        if (normalized.Length <= TokenPrefixThreshold)
        {
            return await SearchWithTokenPrefixAsync(normalized, ct).ConfigureAwait(false);
        }

        // For longer queries, use standard fuzzy matching
        var apps = await _appRepository.GetAllAsync(ct).ConfigureAwait(false);
        if (apps.Count == 0)
        {
            return Array.Empty<SearchResult>();
        }

        var results = new List<SearchResult>(Math.Min(MaxResults, apps.Count));
        foreach (var app in apps)
        {
            ct.ThrowIfCancellationRequested();

            var score = _scorer.Score(normalized, app.Name, out var matchKind);
            if (score <= 0)
            {
                continue;
            }

            results.Add(new SearchResult
            {
                App = app,
                Score = score,
                MatchKind = matchKind
            });
        }

        if (results.Count == 0)
        {
            return Array.Empty<SearchResult>();
        }

        results.Sort(CompareResults);

        if (results.Count > MaxResults)
        {
            results.RemoveRange(MaxResults, results.Count - MaxResults);
        }

        _logger.LogDebug("Search '{Query}' returned {Count} results.", normalized, results.Count);
        return results;
    }

    private async Task<IReadOnlyList<SearchResult>> SearchWithTokenPrefixAsync(string query, CancellationToken ct)
    {
        var entries = await _appRepository.GetAllWithTokensAsync(ct).ConfigureAwait(false);
        if (entries.Count == 0)
        {
            return Array.Empty<SearchResult>();
        }

        var results = new List<SearchResult>(Math.Min(MaxResults, entries.Count));
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();

            // Check if any token starts with the query prefix
            var hasMatchingToken = false;
            foreach (var token in entry.Tokens)
            {
                if (token.StartsWith(query, StringComparison.OrdinalIgnoreCase))
                {
                    hasMatchingToken = true;
                    break;
                }
            }

            // Skip if no token matches the prefix
            if (!hasMatchingToken)
            {
                continue;
            }

            var score = _scorer.Score(query, entry.App.Name, out var matchKind);
            if (score <= 0)
            {
                continue;
            }

            results.Add(new SearchResult
            {
                App = entry.App,
                Score = score,
                MatchKind = matchKind
            });
        }

        if (results.Count == 0)
        {
            return Array.Empty<SearchResult>();
        }

        results.Sort(CompareResults);

        if (results.Count > MaxResults)
        {
            results.RemoveRange(MaxResults, results.Count - MaxResults);
        }

        _logger.LogDebug("Token prefix search '{Query}' returned {Count} results.", query, results.Count);
        return results;
    }

    private static int CompareResults(SearchResult left, SearchResult right)
    {
        var scoreCompare = right.Score.CompareTo(left.Score);
        if (scoreCompare != 0)
        {
            return scoreCompare;
        }

        var lengthCompare = left.App.Name.Length.CompareTo(right.App.Name.Length);
        if (lengthCompare != 0)
        {
            return lengthCompare;
        }

        var nameCompare = StringComparer.OrdinalIgnoreCase.Compare(left.App.Name, right.App.Name);
        if (nameCompare != 0)
        {
            return nameCompare;
        }

        return StringComparer.OrdinalIgnoreCase.Compare(left.App.ExecutablePath, right.App.ExecutablePath);
    }
}
