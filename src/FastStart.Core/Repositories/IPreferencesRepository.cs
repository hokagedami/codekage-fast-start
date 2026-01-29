using FastStart.Core.Models;

namespace FastStart.Core.Repositories;

/// <summary>
/// Provides access to user preferences storage.
/// </summary>
public interface IPreferencesRepository
{
    /// <summary>
    /// Reads a preference value.
    /// </summary>
    Task<PreferenceInfo?> GetAsync(string key, CancellationToken ct);

    /// <summary>
    /// Upserts a preference value.
    /// </summary>
    Task UpsertAsync(PreferenceInfo preference, CancellationToken ct);
}
