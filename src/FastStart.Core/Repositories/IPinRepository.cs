using FastStart.Core.Models;

namespace FastStart.Core.Repositories;

/// <summary>
/// Provides access to pinned application storage.
/// </summary>
public interface IPinRepository
{
    /// <summary>
    /// Gets all pins ordered by position.
    /// </summary>
    Task<IReadOnlyList<PinInfo>> GetPinsAsync(CancellationToken ct);

    /// <summary>
    /// Persists pin order entries.
    /// </summary>
    Task UpsertPinsAsync(IReadOnlyList<PinInfo> pins, CancellationToken ct);

    /// <summary>
    /// Adds a pin for the specified application.
    /// </summary>
    Task AddPinAsync(long applicationId, CancellationToken ct);

    /// <summary>
    /// Removes a pin for the specified application.
    /// </summary>
    Task RemovePinAsync(long applicationId, CancellationToken ct);

    /// <summary>
    /// Checks if an application is pinned.
    /// </summary>
    Task<bool> IsPinnedAsync(long applicationId, CancellationToken ct);
}
