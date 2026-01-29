using System.Diagnostics;

namespace FastStart.Core.Services;

/// <summary>
/// Default implementation of startup timing collection.
/// </summary>
public sealed class StartupTiming : IStartupTiming
{
    private readonly Stopwatch _stopwatch;
    private readonly long[] _ticks;

    /// <summary>
    /// Initializes a new instance of the <see cref="StartupTiming"/> class.
    /// </summary>
    public StartupTiming(Stopwatch stopwatch)
    {
        _stopwatch = stopwatch;
        _ticks = new long[(int)StartupMarker.Count];
    }

    /// <inheritdoc />
    public void Mark(StartupMarker marker)
    {
        var index = (int)marker;
        if (index < 0 || index >= _ticks.Length)
        {
            return;
        }

        Interlocked.Exchange(ref _ticks[index], _stopwatch.ElapsedTicks);
    }

    /// <inheritdoc />
    public bool TryGetElapsed(StartupMarker marker, out TimeSpan elapsed)
    {
        var index = (int)marker;
        if (index < 0 || index >= _ticks.Length)
        {
            elapsed = TimeSpan.Zero;
            return false;
        }

        var ticks = Interlocked.Read(ref _ticks[index]);
        if (ticks <= 0)
        {
            elapsed = TimeSpan.Zero;
            return false;
        }

        elapsed = TimeSpan.FromTicks(ticks);
        return true;
    }
}
