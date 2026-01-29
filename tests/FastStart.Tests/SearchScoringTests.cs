using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FastStart.Tests;

public sealed class SearchScoringTests
{
    [Fact]
    public void FuzzyScorer_PrefersExactOverPrefix()
    {
        var scorer = new FuzzyScorer();
        var exactScore = scorer.Score("Notepad", "Notepad", out var exactKind);
        var prefixScore = scorer.Score("Note", "Notepad", out var prefixKind);
        var fuzzyScore = scorer.Score("npd", "Notepad", out var fuzzyKind);

        Assert.Equal(SearchMatchKind.Exact, exactKind);
        Assert.Equal(SearchMatchKind.Prefix, prefixKind);
        Assert.Equal(SearchMatchKind.Fuzzy, fuzzyKind);
        Assert.True(exactScore > prefixScore);
        Assert.True(prefixScore > fuzzyScore);
    }

    [Fact]
    public async Task SearchService_OrdersDeterministically()
    {
        var apps = new[]
        {
            new AppInfo
            {
                Name = "Alpha",
                ExecutablePath = "C:\\Apps\\alpha-b.exe",
                Source = AppSource.Shortcut,
                LastIndexedUtc = DateTimeOffset.UtcNow
            },
            new AppInfo
            {
                Name = "Alpha",
                ExecutablePath = "C:\\Apps\\alpha-a.exe",
                Source = AppSource.Shortcut,
                LastIndexedUtc = DateTimeOffset.UtcNow
            }
        };

        var repository = new InMemoryAppRepository(apps);
        var service = new SearchService(repository, new FuzzyScorer(), NullLogger<SearchService>.Instance);

        var results = await service.SearchAsync("alpha", CancellationToken.None);

        Assert.Equal(2, results.Count);
        Assert.Equal("C:\\Apps\\alpha-a.exe", results[0].App.ExecutablePath);
        Assert.Equal("C:\\Apps\\alpha-b.exe", results[1].App.ExecutablePath);
    }

    private sealed class InMemoryAppRepository : IAppRepository
    {
        private readonly IReadOnlyList<AppInfo> _apps;
        private readonly IReadOnlyList<AppIndexEntry> _entries;

        public InMemoryAppRepository(IReadOnlyList<AppInfo> apps)
        {
            _apps = apps;
            _entries = apps.Select(a => new AppIndexEntry
            {
                App = a,
                Tokens = a.Name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            }).ToArray();
        }

        public Task<IReadOnlyList<AppInfo>> GetAllAsync(CancellationToken ct) => Task.FromResult(_apps);

        public Task<IReadOnlyList<AppIndexEntry>> GetAllWithTokensAsync(CancellationToken ct) => Task.FromResult(_entries);

        public Task UpsertAppsWithTokensAsync(IReadOnlyList<AppIndexEntry> entries, CancellationToken ct) => Task.CompletedTask;
    }
}
