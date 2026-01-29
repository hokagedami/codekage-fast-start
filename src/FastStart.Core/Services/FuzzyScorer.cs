using FastStart.Core.Models;

namespace FastStart.Core.Services;

/// <summary>
/// Scores query and candidate pairs for deterministic ranking.
/// </summary>
public sealed class FuzzyScorer
{
    /// <summary>
    /// Computes a score for the query against the candidate text.
    /// </summary>
    public int Score(string query, string candidate, out SearchMatchKind matchKind)
    {
        matchKind = SearchMatchKind.None;

        if (string.IsNullOrWhiteSpace(query) || string.IsNullOrWhiteSpace(candidate))
        {
            return 0;
        }

        var querySpan = query.AsSpan();
        var candidateSpan = candidate.AsSpan();

        if (candidateSpan.Equals(querySpan, StringComparison.OrdinalIgnoreCase))
        {
            matchKind = SearchMatchKind.Exact;
            return 1000;
        }

        if (candidateSpan.StartsWith(querySpan, StringComparison.OrdinalIgnoreCase))
        {
            matchKind = SearchMatchKind.Prefix;
            return 850 - Math.Min(200, candidateSpan.Length - querySpan.Length);
        }

        var substringIndex = IndexOf(candidateSpan, querySpan);
        if (substringIndex >= 0)
        {
            matchKind = SearchMatchKind.Substring;
            return 700 - Math.Min(300, substringIndex * 4);
        }

        var fuzzyScore = ScoreFuzzy(querySpan, candidateSpan);
        if (fuzzyScore > 0)
        {
            matchKind = SearchMatchKind.Fuzzy;
        }

        return fuzzyScore;
    }

    private static int ScoreFuzzy(ReadOnlySpan<char> query, ReadOnlySpan<char> candidate)
    {
        var score = 400;
        var lastIndex = -1;
        var consecutive = 0;

        for (var i = 0; i < query.Length; i++)
        {
            var nextIndex = IndexOfChar(candidate, query[i], lastIndex + 1);
            if (nextIndex < 0)
            {
                return 0;
            }

            if (nextIndex == lastIndex + 1)
            {
                consecutive++;
                score += 15 * consecutive;
            }
            else
            {
                consecutive = 0;
                var gap = nextIndex - lastIndex;
                score -= Math.Min(30, gap);
            }

            lastIndex = nextIndex;
        }

        score -= Math.Min(100, candidate.Length / 2);
        return score;
    }

    private static int IndexOf(ReadOnlySpan<char> candidate, ReadOnlySpan<char> query)
    {
        for (var i = 0; i <= candidate.Length - query.Length; i++)
        {
            if (candidate.Slice(i, query.Length).Equals(query, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static int IndexOfChar(ReadOnlySpan<char> candidate, char value, int startIndex)
    {
        var lower = char.ToLowerInvariant(value);
        for (var i = startIndex; i < candidate.Length; i++)
        {
            if (char.ToLowerInvariant(candidate[i]) == lower)
            {
                return i;
            }
        }

        return -1;
    }
}
