namespace FastStart.Core.Services;

/// <summary>
/// Captures startup milestone timings.
/// </summary>
public interface IStartupTiming
{
    /// <summary>
    /// Records a startup marker.
    /// </summary>
    void Mark(StartupMarker marker);

    /// <summary>
    /// Tries to read an elapsed timestamp for a marker.
    /// </summary>
    bool TryGetElapsed(StartupMarker marker, out TimeSpan elapsed);
}
