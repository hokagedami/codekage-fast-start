namespace FastStart.Core.Services;

/// <summary>
/// Default application tokenizer for search indexing.
/// </summary>
public sealed class AppTokenizer : IAppTokenizer
{
    /// <inheritdoc />
    public IReadOnlyList<string> Tokenize(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Array.Empty<string>();
        }

        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var span = text.AsSpan();
        var start = 0;

        for (var i = 0; i <= span.Length; i++)
        {
            var isEnd = i == span.Length;
            var isSeparator = !isEnd && !char.IsLetterOrDigit(span[i]);
            if (!isEnd && !isSeparator)
            {
                continue;
            }

            if (i > start)
            {
                var token = span.Slice(start, i - start).ToString().ToLowerInvariant();
                tokens.Add(token);
            }

            start = i + 1;
        }

        return tokens.Count == 0 ? Array.Empty<string>() : tokens.ToArray();
    }
}
