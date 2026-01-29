using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace FastStart.Data.Repositories;

/// <summary>
/// EF Core implementation of application persistence.
/// </summary>
public sealed class AppRepository : IAppRepository
{
    private readonly IDbContextFactory<FastStartDbContext> _dbContextFactory;
    private readonly ILogger<AppRepository> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AppRepository"/> class.
    /// </summary>
    public AppRepository(IDbContextFactory<FastStartDbContext> dbContextFactory, ILogger<AppRepository> logger)
    {
        _dbContextFactory = dbContextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppInfo>> GetAllAsync(CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var apps = await db.Applications
            .AsNoTracking()
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var result = new List<AppInfo>(apps.Count);
        foreach (var app in apps)
        {
            result.Add(ToModel(app));
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AppIndexEntry>> GetAllWithTokensAsync(CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var apps = await db.Applications
            .AsNoTracking()
            .Include(a => a.Tokens)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var result = new List<AppIndexEntry>(apps.Count);
        foreach (var app in apps)
        {
            var tokens = app.Tokens?.Select(t => t.Token).ToArray() ?? Array.Empty<string>();
            result.Add(new AppIndexEntry
            {
                App = ToModel(app),
                Tokens = tokens
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task UpsertAppsWithTokensAsync(IReadOnlyList<AppIndexEntry> entries, CancellationToken ct)
    {
        if (entries.Count == 0)
        {
            return;
        }

        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        await using var transaction = await db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);

        var entityList = new List<AppEntity>(entries.Count);
        var entryByKey = new Dictionary<(string, string?), AppIndexEntry>(entries.Count);

        // De-duplicate entries - keep the last occurrence of each unique (ExecutablePath, Arguments) pair
        foreach (var entry in entries)
        {
            var key = (entry.App.ExecutablePath, entry.App.Arguments);
            entryByKey[key] = entry;
        }

        foreach (var entry in entryByKey.Values)
        {
            ct.ThrowIfCancellationRequested();

            var app = entry.App;
            var existing = await db.Applications
                .FirstOrDefaultAsync(a => a.ExecutablePath == app.ExecutablePath && a.Arguments == app.Arguments, ct)
                .ConfigureAwait(false);

            if (existing is null)
            {
                existing = new AppEntity();
                db.Applications.Add(existing);
            }

            existing.Name = app.Name;
            existing.ExecutablePath = app.ExecutablePath;
            existing.Arguments = app.Arguments;
            existing.WorkingDirectory = app.WorkingDirectory;
            existing.IconPath = app.IconPath;
            existing.Source = app.Source;
            existing.PackageFamilyName = app.PackageFamilyName;
            existing.LastIndexedUtc = app.LastIndexedUtc;

            entityList.Add(existing);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);

        var appIds = new long[entityList.Count];
        for (var i = 0; i < entityList.Count; i++)
        {
            appIds[i] = entityList[i].Id;
        }

        var existingTokens = db.AppTokens.Where(token => appIds.Contains(token.AppId));
        db.AppTokens.RemoveRange(existingTokens);

        var tokenEntities = new List<AppTokenEntity>(entityList.Count * 4);
        var deduplicatedEntries = entryByKey.Values.ToArray();
        for (var i = 0; i < deduplicatedEntries.Length; i++)
        {
            foreach (var token in deduplicatedEntries[i].Tokens)
            {
                tokenEntities.Add(new AppTokenEntity
                {
                    AppId = entityList[i].Id,
                    Token = token
                });
            }
        }

        if (tokenEntities.Count > 0)
        {
            await db.AppTokens.AddRangeAsync(tokenEntities, ct).ConfigureAwait(false);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        await transaction.CommitAsync(ct).ConfigureAwait(false);

        _logger.LogInformation("Upserted {Count} applications.", entityList.Count);
    }

    private static AppInfo ToModel(AppEntity entity)
    {
        return new AppInfo
        {
            Id = entity.Id,
            Name = entity.Name,
            ExecutablePath = entity.ExecutablePath,
            Arguments = entity.Arguments,
            WorkingDirectory = entity.WorkingDirectory,
            IconPath = entity.IconPath,
            Source = entity.Source,
            PackageFamilyName = entity.PackageFamilyName,
            LastIndexedUtc = entity.LastIndexedUtc
        };
    }
}
