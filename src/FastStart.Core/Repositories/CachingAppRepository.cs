using FastStart.Core.Models;
using Microsoft.Extensions.Logging;

namespace FastStart.Core.Repositories;

/// <summary>
/// In-memory caching decorator for IAppRepository.
/// Provides fast access to indexed applications for search operations.
/// </summary>
public sealed class CachingAppRepository : IAppRepository, IDisposable
{
    private readonly IAppRepository _inner;
    private readonly ILogger<CachingAppRepository> _logger;
    private readonly ReaderWriterLockSlim _cacheLock = new();
    private IReadOnlyList<AppInfo>? _cache;
    private IReadOnlyList<AppIndexEntry>? _cacheWithTokens;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="CachingAppRepository"/> class.
    /// </summary>
    public CachingAppRepository(IAppRepository inner, ILogger<CachingAppRepository> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppInfo>> GetAllAsync(CancellationToken ct)
    {
        // Fast path: check cache under read lock
        _cacheLock.EnterReadLock();
        try
        {
            if (_cache is not null)
            {
                return _cache;
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        // Slow path: load from database and populate cache
        var apps = await _inner.GetAllAsync(ct).ConfigureAwait(false);

        _cacheLock.EnterWriteLock();
        try
        {
            // Double-check pattern
            _cache ??= apps;
            return _cache;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppIndexEntry>> GetAllWithTokensAsync(CancellationToken ct)
    {
        // Fast path: check cache under read lock
        _cacheLock.EnterReadLock();
        try
        {
            if (_cacheWithTokens is not null)
            {
                return _cacheWithTokens;
            }
        }
        finally
        {
            _cacheLock.ExitReadLock();
        }

        // Slow path: load from database and populate cache
        var entries = await _inner.GetAllWithTokensAsync(ct).ConfigureAwait(false);

        _cacheLock.EnterWriteLock();
        try
        {
            // Double-check pattern
            _cacheWithTokens ??= entries;
            return _cacheWithTokens;
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public async Task UpsertAppsWithTokensAsync(IReadOnlyList<AppIndexEntry> entries, CancellationToken ct)
    {
        await _inner.UpsertAppsWithTokensAsync(entries, ct).ConfigureAwait(false);

        // Invalidate cache after successful upsert
        _cacheLock.EnterWriteLock();
        try
        {
            _cache = null;
            _cacheWithTokens = null;
            _logger.LogDebug("App cache invalidated after upsert.");
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Invalidates the cache, forcing a reload from the database on next access.
    /// </summary>
    public void Invalidate()
    {
        _cacheLock.EnterWriteLock();
        try
        {
            _cache = null;
            _cacheWithTokens = null;
            _logger.LogDebug("App cache manually invalidated.");
        }
        finally
        {
            _cacheLock.ExitWriteLock();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _cacheLock.Dispose();
        _disposed = true;
    }
}
