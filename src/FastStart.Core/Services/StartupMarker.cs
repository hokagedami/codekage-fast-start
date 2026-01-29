namespace FastStart.Core.Services;

/// <summary>
/// Identifies startup milestones.
/// </summary>
public enum StartupMarker
{
    ProcessStart = 0,
    HostBuilt = 1,
    HostStarting = 2,
    HostStarted = 3,
    WindowCreated = 4,
    FirstWindowActivated = 5,
    Count = 6
}
