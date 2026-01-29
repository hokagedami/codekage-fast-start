namespace FastStart.Core.Services;

/// <summary>
/// Creates normalized search tokens for application metadata.
/// </summary>
public interface IAppTokenizer
{
    /// <summary>
    /// Tokenizes input text.
    /// </summary>
    IReadOnlyList<string> Tokenize(string text);
}
