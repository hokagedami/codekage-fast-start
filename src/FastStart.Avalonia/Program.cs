using System;
using System.Diagnostics;
using Avalonia;
using FastStart.Core.Services;

namespace FastStart.Avalonia;

internal sealed class Program
{
    public static IStartupTiming StartupTiming { get; private set; } = null!;
    public static bool DiagnosticMode { get; private set; }
    public static bool StartMinimized { get; private set; }

    [STAThread]
    public static void Main(string[] args)
    {
        StartupTiming = new StartupTiming(Stopwatch.StartNew());
        StartupTiming.Mark(StartupMarker.ProcessStart);

        DiagnosticMode = Array.Exists(args, a => a == "--diag");
        StartMinimized = Array.Exists(args, a => a == "--background");

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
