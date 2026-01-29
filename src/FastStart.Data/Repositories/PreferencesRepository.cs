using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FastStart.Data.Repositories;

/// <summary>
/// EF Core implementation of preference persistence.
/// </summary>
public sealed class PreferencesRepository : IPreferencesRepository
{
    private readonly IDbContextFactory<FastStartDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PreferencesRepository"/> class.
    /// </summary>
    public PreferencesRepository(IDbContextFactory<FastStartDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public async Task<PreferenceInfo?> GetAsync(string key, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var preference = await db.Preferences.AsNoTracking().FirstOrDefaultAsync(p => p.Key == key, ct).ConfigureAwait(false);
        return preference is null ? null : new PreferenceInfo(preference.Key, preference.Value, preference.UpdatedAtUtc);
    }

    /// <inheritdoc />
    public async Task UpsertAsync(PreferenceInfo preference, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var existing = await db.Preferences.FirstOrDefaultAsync(p => p.Key == preference.Key, ct).ConfigureAwait(false);
        if (existing is null)
        {
            db.Preferences.Add(new PreferenceEntity
            {
                Key = preference.Key,
                Value = preference.Value,
                UpdatedAtUtc = preference.UpdatedAtUtc
            });
        }
        else
        {
            existing.Value = preference.Value;
            existing.UpdatedAtUtc = preference.UpdatedAtUtc;
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
