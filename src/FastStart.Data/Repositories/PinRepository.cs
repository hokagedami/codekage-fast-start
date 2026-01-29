using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FastStart.Data.Repositories;

/// <summary>
/// EF Core implementation of pin persistence.
/// </summary>
public sealed class PinRepository : IPinRepository
{
    private readonly IDbContextFactory<FastStartDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="PinRepository"/> class.
    /// </summary>
    public PinRepository(IDbContextFactory<FastStartDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<PinInfo>> GetPinsAsync(CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var pins = await db.UserPins
            .AsNoTracking()
            .Include(pin => pin.Application)
            .OrderBy(pin => pin.Position)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var result = new List<PinInfo>(pins.Count);
        foreach (var pin in pins)
        {
            result.Add(new PinInfo
            {
                ApplicationId = pin.ApplicationId,
                Position = pin.Position,
                GroupName = pin.GroupName,
                App = pin.Application is null ? null : new AppInfo
                {
                    Id = pin.Application.Id,
                    Name = pin.Application.Name,
                    ExecutablePath = pin.Application.ExecutablePath,
                    Arguments = pin.Application.Arguments,
                    WorkingDirectory = pin.Application.WorkingDirectory,
                    IconPath = pin.Application.IconPath,
                    Source = pin.Application.Source,
                    PackageFamilyName = pin.Application.PackageFamilyName,
                    LastIndexedUtc = pin.Application.LastIndexedUtc
                }
            });
        }

        return result;
    }

    /// <inheritdoc />
    public async Task UpsertPinsAsync(IReadOnlyList<PinInfo> pins, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        db.UserPins.RemoveRange(db.UserPins);

        var entities = new List<PinEntity>(pins.Count);
        foreach (var pin in pins)
        {
            entities.Add(new PinEntity
            {
                ApplicationId = pin.ApplicationId,
                Position = pin.Position,
                GroupName = pin.GroupName
            });
        }

        if (entities.Count > 0)
        {
            await db.UserPins.AddRangeAsync(entities, ct).ConfigureAwait(false);
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddPinAsync(long applicationId, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        // Check if already pinned
        var exists = await db.UserPins.AnyAsync(p => p.ApplicationId == applicationId, ct).ConfigureAwait(false);
        if (exists)
            return;

        // Get the next position
        var maxPosition = await db.UserPins.MaxAsync(p => (int?)p.Position, ct).ConfigureAwait(false) ?? -1;

        db.UserPins.Add(new PinEntity
        {
            ApplicationId = applicationId,
            Position = maxPosition + 1
        });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task RemovePinAsync(long applicationId, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);

        var pin = await db.UserPins.FirstOrDefaultAsync(p => p.ApplicationId == applicationId, ct).ConfigureAwait(false);
        if (pin is not null)
        {
            db.UserPins.Remove(pin);
            await db.SaveChangesAsync(ct).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsPinnedAsync(long applicationId, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        return await db.UserPins.AnyAsync(p => p.ApplicationId == applicationId, ct).ConfigureAwait(false);
    }
}
