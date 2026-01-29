using FastStart.Core.Models;
using FastStart.Core.Repositories;

namespace FastStart.Core.Services;

/// <summary>
/// Default telemetry consent storage.
/// </summary>
public sealed class TelemetryConsentService : ITelemetryConsentService
{
    private const string TelemetryKey = "telemetry.enabled";
    private readonly IPreferencesRepository _preferencesRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="TelemetryConsentService"/> class.
    /// </summary>
    public TelemetryConsentService(IPreferencesRepository preferencesRepository)
    {
        _preferencesRepository = preferencesRepository;
    }

    /// <inheritdoc />
    public async ValueTask<bool> IsEnabledAsync(CancellationToken ct)
    {
        var preference = await _preferencesRepository.GetAsync(TelemetryKey, ct).ConfigureAwait(false);
        return preference?.Value.Equals("true", StringComparison.OrdinalIgnoreCase) == true;
    }

    /// <inheritdoc />
    public Task SetEnabledAsync(bool enabled, CancellationToken ct)
    {
        var preference = new PreferenceInfo(TelemetryKey, enabled ? "true" : "false", DateTimeOffset.UtcNow);
        return _preferencesRepository.UpsertAsync(preference, ct);
    }
}
