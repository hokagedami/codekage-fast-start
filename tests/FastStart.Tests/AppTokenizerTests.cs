using FastStart.Core.Services;
using Xunit;

namespace FastStart.Tests;

public sealed class AppTokenizerTests
{
    private readonly AppTokenizer _tokenizer = new();

    [Fact]
    public void Tokenize_SplitsOnSpaces()
    {
        var tokens = _tokenizer.Tokenize("Microsoft Word");

        Assert.Contains("microsoft", tokens);
        Assert.Contains("word", tokens);
    }

    [Fact]
    public void Tokenize_SplitsOnSpecialCharacters()
    {
        var tokens = _tokenizer.Tokenize("Visual-Studio_2022");

        Assert.Contains("visual", tokens);
        Assert.Contains("studio", tokens);
        Assert.Contains("2022", tokens);
    }

    [Fact]
    public void Tokenize_LowercasesTokens()
    {
        var tokens = _tokenizer.Tokenize("NOTEPAD");

        Assert.Single(tokens);
        Assert.Equal("notepad", tokens[0]);
    }

    [Fact]
    public void Tokenize_DeduplicatesTokens()
    {
        var tokens = _tokenizer.Tokenize("Test test TEST");

        Assert.Single(tokens);
        Assert.Equal("test", tokens[0]);
    }

    [Fact]
    public void Tokenize_HandlesEmptyString()
    {
        var tokens = _tokenizer.Tokenize(string.Empty);

        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_HandlesNullString()
    {
        var tokens = _tokenizer.Tokenize(null!);

        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_HandlesWhitespaceOnly()
    {
        var tokens = _tokenizer.Tokenize("   ");

        Assert.Empty(tokens);
    }

    [Fact]
    public void Tokenize_HandlesSpecialCharactersOnly()
    {
        var tokens = _tokenizer.Tokenize("---");

        Assert.Empty(tokens);
    }
}
