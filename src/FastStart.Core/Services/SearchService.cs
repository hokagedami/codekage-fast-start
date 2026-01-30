using System.Buffers;
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
    private const int TokenPrefixThreshold = 3;
    private readonly IAppRepository _appRepository;
    private readonly FuzzyScorer _scorer;
    private readonly ILogger<SearchService> _logger;

    // Cached data to avoid repeated allocations
    private IReadOnlyList<AppInfo>? _cachedApps;
    private IReadOnlyList<AppIndexEntry>? _cachedEntries;
    private readonly object _cacheLock = new();

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
        if (normalized.Length == 0)
        {
            return Array.Empty<SearchResult>();
        }

        // For short queries, use token-based prefix filtering
        if (normalized.Length <= TokenPrefixThreshold)
        {
            return await SearchWithTokenPrefixAsync(normalized, ct).ConfigureAwait(false);
        }

        // For longer queries, use fuzzy matching
        var apps = await GetCachedAppsAsync(ct).ConfigureAwait(false);
        if (apps.Count == 0)
        {
            return Array.Empty<SearchResult>();
        }

        return SearchFuzzy(apps, normalized);
    }

    private SearchResult[] SearchFuzzy(IReadOnlyList<AppInfo> apps, string query)
    {
        // Rent array from pool to avoid allocation
        var buffer = ArrayPool<(int Score, int Index, SearchMatchKind MatchKind)>.Shared.Rent(apps.Count);
        var count = 0;

        try
        {
            for (var i = 0; i < apps.Count; i++)
            {
                var score = _scorer.Score(query, apps[i].Name, out var matchKind);
                if (score > 0)
                {
                    buffer[count++] = (score, i, matchKind);
                }
            }

            if (count == 0)
            {
                return Array.Empty<SearchResult>();
            }

            // Sort by score descending
            Array.Sort(buffer, 0, count, ScoredIndexComparer.Instance);

            // Take top results
            var resultCount = Math.Min(count, MaxResults);
            var results = new SearchResult[resultCount];

            for (var i = 0; i < resultCount; i++)
            {
                var (score, index, matchKind) = buffer[i];
                results[i] = new SearchResult
                {
                    App = apps[index],
                    Score = score,
                    MatchKind = matchKind
                };
            }

            return results;
        }
        finally
        {
            ArrayPool<(int, int, SearchMatchKind)>.Shared.Return(buffer);
        }
    }

    private async Task<IReadOnlyList<SearchResult>> SearchWithTokenPrefixAsync(string query, CancellationToken ct)
    {
        var entries = await GetCachedEntriesAsync(ct).ConfigureAwait(false);
        if (entries.Count == 0)
        {
            return Array.Empty<SearchResult>();
        }

        var buffer = ArrayPool<(int Score, int Index, SearchMatchKind MatchKind)>.Shared.Rent(entries.Count);
        var count = 0;

        try
        {
            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

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

                if (!hasMatchingToken)
                {
                    continue;
                }

                var score = _scorer.Score(query, entry.App.Name, out var matchKind);
                if (score > 0)
                {
                    buffer[count++] = (score, i, matchKind);
                }
            }

            if (count == 0)
            {
                return Array.Empty<SearchResult>();
            }

            Array.Sort(buffer, 0, count, ScoredIndexComparer.Instance);

            var resultCount = Math.Min(count, MaxResults);
            var results = new SearchResult[resultCount];

            for (var i = 0; i < resultCount; i++)
            {
                var (score, index, matchKind) = buffer[i];
                results[i] = new SearchResult
                {
                    App = entries[index].App,
                    Score = score,
                    MatchKind = matchKind
                };
            }

            return results;
        }
        finally
        {
            ArrayPool<(int, int, SearchMatchKind)>.Shared.Return(buffer);
        }
    }

    private async Task<IReadOnlyList<AppInfo>> GetCachedAppsAsync(CancellationToken ct)
    {
        if (_cachedApps is not null)
        {
            return _cachedApps;
        }

        var apps = await _appRepository.GetAllAsync(ct).ConfigureAwait(false);

        lock (_cacheLock)
        {
            _cachedApps ??= apps;
        }

        return _cachedApps;
    }

    private async Task<IReadOnlyList<AppIndexEntry>> GetCachedEntriesAsync(CancellationToken ct)
    {
        if (_cachedEntries is not null)
        {
            return _cachedEntries;
        }

        var entries = await _appRepository.GetAllWithTokensAsync(ct).ConfigureAwait(false);

        lock (_cacheLock)
        {
            _cachedEntries ??= entries;
        }

        return _cachedEntries;
    }

    /// <summary>
    /// Invalidates the cached app data. Call after indexing completes.
    /// </summary>
    public void InvalidateCache()
    {
        lock (_cacheLock)
        {
            _cachedApps = null;
            _cachedEntries = null;
        }
    }

    private sealed class ScoredIndexComparer : IComparer<(int Score, int Index, SearchMatchKind MatchKind)>
    {
        public static readonly ScoredIndexComparer Instance = new();

        public int Compare((int Score, int Index, SearchMatchKind MatchKind) x, (int Score, int Index, SearchMatchKind MatchKind) y)
        {
            // Sort by score descending
            return y.Score.CompareTo(x.Score);
        }
    }
}
