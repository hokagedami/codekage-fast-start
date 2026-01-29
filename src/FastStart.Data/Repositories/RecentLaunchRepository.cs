using FastStart.Core.Models;
using FastStart.Core.Repositories;
using FastStart.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace FastStart.Data.Repositories;

/// <summary>
/// EF Core implementation of recent launch persistence.
/// </summary>
public sealed class RecentLaunchRepository : IRecentLaunchRepository
{
    private readonly IDbContextFactory<FastStartDbContext> _dbContextFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="RecentLaunchRepository"/> class.
    /// </summary>
    public RecentLaunchRepository(IDbContextFactory<FastStartDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc />
    public async Task AddAsync(RecentLaunchInfo launch, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        db.RecentLaunches.Add(new RecentLaunchEntity
        {
            ApplicationId = launch.ApplicationId,
            LaunchedAtUtc = launch.LaunchedAtUtc,
            SearchQuery = launch.SearchQuery
        });

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RecentLaunchInfo>> GetRecentAsync(int take, CancellationToken ct)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(ct).ConfigureAwait(false);
        var launches = await db.RecentLaunches
            .AsNoTracking()
            .Include(launch => launch.Application)
            .OrderByDescending(launch => launch.LaunchedAtUtc)
            .Take(take)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        var result = new List<RecentLaunchInfo>(launches.Count);
        foreach (var launch in launches)
        {
            result.Add(new RecentLaunchInfo
            {
                ApplicationId = launch.ApplicationId,
                LaunchedAtUtc = launch.LaunchedAtUtc,
                SearchQuery = launch.SearchQuery,
                App = launch.Application is null ? null : new AppInfo
                {
                    Id = launch.Application.Id,
                    Name = launch.Application.Name,
                    ExecutablePath = launch.Application.ExecutablePath,
                    Arguments = launch.Application.Arguments,
                    WorkingDirectory = launch.Application.WorkingDirectory,
                    IconPath = launch.Application.IconPath,
                    Source = launch.Application.Source,
                    PackageFamilyName = launch.Application.PackageFamilyName,
                    LastIndexedUtc = launch.Application.LastIndexedUtc
                }
            });
        }

        return result;
    }
}
