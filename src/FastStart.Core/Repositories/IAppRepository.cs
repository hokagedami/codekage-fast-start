using FastStart.Core.Models;

namespace FastStart.Core.Repositories;

/// <summary>
/// Provides access to application persistence.
/// </summary>
public interface IAppRepository
{
    /// <summary>
    /// Gets all indexed applications.
    /// </summary>
    Task<IReadOnlyList<AppInfo>> GetAllAsync(CancellationToken ct);

    /// <summary>
    /// Gets all indexed applications with their search tokens.
    /// </summary>
    Task<IReadOnlyList<AppIndexEntry>> GetAllWithTokensAsync(CancellationToken ct);

    /// <summary>
    /// Upserts applications and their tokens.
    /// </summary>
    Task UpsertAppsWithTokensAsync(IReadOnlyList<AppIndexEntry> entries, CancellationToken ct);
}
