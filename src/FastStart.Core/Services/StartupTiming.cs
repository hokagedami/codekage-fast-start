using System.Diagnostics;
using System.Text;

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

    /// <summary>
    /// Gets the time to first window visible (FirstWindowActivated marker).
    /// </summary>
    public TimeSpan? TimeToVisible =>
        TryGetElapsed(StartupMarker.FirstWindowActivated, out var elapsed) ? elapsed : null;

    /// <summary>
    /// Generates a report of all startup timings.
    /// </summary>
    public string GenerateReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== FastStart Startup Timing Report ===");
        sb.AppendLine();

        for (var i = 0; i < (int)StartupMarker.Count; i++)
        {
            var marker = (StartupMarker)i;
            if (TryGetElapsed(marker, out var elapsed))
            {
                sb.AppendLine($"  {marker,-25} {elapsed.TotalMilliseconds,8:F2} ms");
            }
        }

        sb.AppendLine();

        if (TryGetElapsed(StartupMarker.FirstWindowActivated, out var timeToVisible))
        {
            var status = timeToVisible.TotalMilliseconds <= 200 ? "PASS" : "FAIL";
            sb.AppendLine($"  Time to Visible: {timeToVisible.TotalMilliseconds:F2} ms (Target: 150-200ms) [{status}]");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Gets current memory usage statistics.
    /// </summary>
    public static MemoryStats GetMemoryStats()
    {
        var process = Process.GetCurrentProcess();
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        return new MemoryStats
        {
            WorkingSetMB = process.WorkingSet64 / (1024.0 * 1024.0),
            PrivateMemoryMB = process.PrivateMemorySize64 / (1024.0 * 1024.0),
            ManagedMemoryMB = GC.GetTotalMemory(false) / (1024.0 * 1024.0)
        };
    }
}

/// <summary>
/// Memory usage statistics.
/// </summary>
public readonly struct MemoryStats
{
    /// <summary>Working set memory in MB.</summary>
    public required double WorkingSetMB { get; init; }

    /// <summary>Private memory in MB.</summary>
    public required double PrivateMemoryMB { get; init; }

    /// <summary>Managed heap memory in MB.</summary>
    public required double ManagedMemoryMB { get; init; }

    /// <summary>
    /// Generates a memory report.
    /// </summary>
    public string GenerateReport()
    {
        var status = WorkingSetMB < 50 ? "PASS" : "FAIL";
        return $"""
            === FastStart Memory Report ===

              Working Set:    {WorkingSetMB,8:F2} MB (Target: <50MB) [{status}]
              Private Memory: {PrivateMemoryMB,8:F2} MB
              Managed Heap:   {ManagedMemoryMB,8:F2} MB
            """;
    }
}
