using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Core.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace FastStart.Benchmarks;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80)]
public sealed class SearchBenchmarks
{
    private ISearchService _searchService = null!;

    [GlobalSetup]
    public void Setup()
    {
        var apps = new List<AppInfo>(1000);
        for (var i = 0; i < 1000; i++)
        {
            apps.Add(new AppInfo
            {
                Name = $"App {i:0000}",
                ExecutablePath = $"C:\\Apps\\App{i:0000}.exe",
                Source = AppSource.Shortcut,
                LastIndexedUtc = DateTimeOffset.UtcNow
            });
        }

        // Fail threshold: SearchBenchmarks.Search_CommonPrefix Mean < 50ms, Alloc < 100KB.
        var repository = new InMemoryAppRepository(apps);
        _searchService = new SearchService(repository, new FuzzyScorer(), NullLogger<SearchService>.Instance);
    }

    [Benchmark]
    public Task<IReadOnlyList<SearchResult>> Search_CommonPrefix()
    {
        return _searchService.SearchAsync("App 0", CancellationToken.None);
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
