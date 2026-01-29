namespace FastStart.Core.Services;

/// <summary>
/// Stores telemetry consent state.
/// </summary>
public interface ITelemetryConsentService
{
    /// <summary>
    /// Gets whether telemetry is enabled.
    /// </summary>
    ValueTask<bool> IsEnabledAsync(CancellationToken ct);

    /// <summary>
    /// Updates telemetry consent.
    /// </summary>
    Task SetEnabledAsync(bool enabled, CancellationToken ct);
}
