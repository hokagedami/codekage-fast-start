using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FastStart.Tests;

public sealed class TokenPrefixSearchTests
{
    [Fact]
    public async Task ShortQuery_UsesTokenPrefixFiltering()
    {
        var apps = new[]
        {
            CreateEntry("Notepad", "notepad", "text", "editor"),
            CreateEntry("Notes App", "notes", "app"),
            CreateEntry("Calculator", "calc", "math")
        };

        var repository = new TokenAwareRepository(apps);
        var service = new SearchService(repository, new FuzzyScorer(), NullLogger<SearchService>.Instance);

        // Short query "not" should match "Notepad" (notepad token) and "Notes App" (notes token)
        var results = await service.SearchAsync("not", CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.App.Name == "Notepad");
        Assert.Contains(results, r => r.App.Name == "Notes App");
    }

    [Fact]
    public async Task ShortQuery_DoesNotMatchNonPrefixTokens()
    {
        var apps = new[]
        {
            CreateEntry("Notepad", "notepad"),
            CreateEntry("Keynote", "keynote") // "not" is substring but not prefix
        };

        var repository = new TokenAwareRepository(apps);
        var service = new SearchService(repository, new FuzzyScorer(), NullLogger<SearchService>.Instance);

        // "not" should only match "Notepad" (token starts with "not")
        // "Keynote" has "keynote" token which does not start with "not"
        var results = await service.SearchAsync("not", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("Notepad", results[0].App.Name);
    }

    [Fact]
    public async Task LongQuery_UsesStandardFuzzyMatching()
    {
        var apps = new[]
        {
            CreateEntry("Microsoft Word", "microsoft", "word"),
            CreateEntry("Microsoft Excel", "microsoft", "excel")
        };

        var repository = new TokenAwareRepository(apps);
        var service = new SearchService(repository, new FuzzyScorer(), NullLogger<SearchService>.Instance);

        // Long query "word" (4 chars) should use standard fuzzy matching
        var results = await service.SearchAsync("word", CancellationToken.None);

        Assert.Single(results);
        Assert.Equal("Microsoft Word", results[0].App.Name);
    }

    private static AppIndexEntry CreateEntry(string name, params string[] tokens)
    {
        return new AppIndexEntry
        {
            App = new AppInfo
            {
                Name = name,
                ExecutablePath = $"C:\\{name}.exe",
                Source = AppSource.Shortcut,
                LastIndexedUtc = DateTimeOffset.UtcNow
            },
            Tokens = tokens
        };
    }

    private sealed class TokenAwareRepository : IAppRepository
    {
        private readonly IReadOnlyList<AppIndexEntry> _entries;
        private readonly IReadOnlyList<AppInfo> _apps;

        public TokenAwareRepository(IReadOnlyList<AppIndexEntry> entries)
        {
            _entries = entries;
            _apps = entries.Select(e => e.App).ToArray();
        }

        public Task<IReadOnlyList<AppInfo>> GetAllAsync(CancellationToken ct)
            => Task.FromResult(_apps);

        public Task<IReadOnlyList<AppIndexEntry>> GetAllWithTokensAsync(CancellationToken ct)
            => Task.FromResult(_entries);

        public Task UpsertAppsWithTokensAsync(IReadOnlyList<AppIndexEntry> entries, CancellationToken ct)
            => Task.CompletedTask;
    }
}
