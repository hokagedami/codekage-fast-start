using FastStart.Core.Models;

namespace FastStart.Core.Repositories;

/// <summary>
/// Provides access to recent launch storage.
/// </summary>
public interface IRecentLaunchRepository
{
    /// <summary>
    /// Adds a recent launch record.
    /// </summary>
    Task AddAsync(RecentLaunchInfo launch, CancellationToken ct);

    /// <summary>
    /// Gets recent launch records.
    /// </summary>
    Task<IReadOnlyList<RecentLaunchInfo>> GetRecentAsync(int take, CancellationToken ct);
}
