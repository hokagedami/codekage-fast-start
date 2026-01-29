using FastStart.Core.Models;
using FastStart.Core.Repositories;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace FastStart.Tests;

public sealed class CachingAppRepositoryTests : IDisposable
{
    private readonly TrackingAppRepository _inner;
    private readonly CachingAppRepository _cache;

    public CachingAppRepositoryTests()
    {
        _inner = new TrackingAppRepository();
        _cache = new CachingAppRepository(_inner, NullLogger<CachingAppRepository>.Instance);
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    [Fact]
    public async Task GetAllAsync_CachesResults()
    {
        await _cache.GetAllAsync(CancellationToken.None);
        await _cache.GetAllAsync(CancellationToken.None);

        Assert.Equal(1, _inner.GetAllCallCount);
    }

    [Fact]
    public async Task GetAllWithTokensAsync_CachesResults()
    {
        await _cache.GetAllWithTokensAsync(CancellationToken.None);
        await _cache.GetAllWithTokensAsync(CancellationToken.None);

        Assert.Equal(1, _inner.GetAllWithTokensCallCount);
    }

    [Fact]
    public async Task UpsertAppsWithTokensAsync_InvalidatesCache()
    {
        await _cache.GetAllAsync(CancellationToken.None);
        await _cache.UpsertAppsWithTokensAsync(Array.Empty<AppIndexEntry>(), CancellationToken.None);
        await _cache.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, _inner.GetAllCallCount);
    }

    [Fact]
    public async Task Invalidate_ForcesReload()
    {
        await _cache.GetAllAsync(CancellationToken.None);
        _cache.Invalidate();
        await _cache.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, _inner.GetAllCallCount);
    }

    private sealed class TrackingAppRepository : IAppRepository
    {
        private readonly IReadOnlyList<AppInfo> _apps = new[]
        {
            new AppInfo
            {
                Name = "Test App",
                ExecutablePath = "C:\\test.exe",
                Source = AppSource.Shortcut,
                LastIndexedUtc = DateTimeOffset.UtcNow
            }
        };

        public int GetAllCallCount { get; private set; }
        public int GetAllWithTokensCallCount { get; private set; }

        public Task<IReadOnlyList<AppInfo>> GetAllAsync(CancellationToken ct)
        {
            GetAllCallCount++;
            return Task.FromResult(_apps);
        }

        public Task<IReadOnlyList<AppIndexEntry>> GetAllWithTokensAsync(CancellationToken ct)
        {
            GetAllWithTokensCallCount++;
            var entries = _apps.Select(a => new AppIndexEntry
            {
                App = a,
                Tokens = new[] { "test", "app" }
            }).ToArray();
            return Task.FromResult<IReadOnlyList<AppIndexEntry>>(entries);
        }

        public Task UpsertAppsWithTokensAsync(IReadOnlyList<AppIndexEntry> entries, CancellationToken ct)
            => Task.CompletedTask;
    }
}
