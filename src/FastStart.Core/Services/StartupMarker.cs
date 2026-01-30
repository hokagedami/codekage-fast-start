namespace FastStart.Core.Services;

/// <summary>
/// Identifies startup milestones.
/// </summary>
public enum StartupMarker
{
    ProcessStart = 0,
    HostBuilt = 1,
    WindowResolving = 2,
    WindowCreated = 3,
    FirstWindowActivated = 4,
    HostStarting = 5,
    HostStarted = 6,
    Count = 7
}
