using System;
using System.IO.Pipes;
using System.Threading;
using Microsoft.UI.Xaml;

namespace FastStart.UI;

public static class Program
{
    private const string MutexName = "FastStart_SingleInstance_Mutex";
    private const string PipeName = "FastStart_IPC_Pipe";
    private static Mutex? _mutex;

    [STAThread]
    public static void Main(string[] args)
    {
        // Check for single instance
        _mutex = new Mutex(true, MutexName, out var createdNew);

        if (!createdNew)
        {
            // Another instance is running - signal it to show window
            SignalExistingInstance();
            return;
        }

        try
        {
            // Start IPC listener for activation signals
            StartIpcListener();

            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start(_ =>
            {
                var context = new Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(
                    Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                new App();
            });
        }
        finally
        {
            _mutex?.ReleaseMutex();
            _mutex?.Dispose();
        }
    }

    private static void SignalExistingInstance()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(1000); // 1 second timeout
            using var writer = new System.IO.StreamWriter(client);
            writer.WriteLine("SHOW");
            writer.Flush();
        }
        catch
        {
            // Failed to connect - instance may be starting up
        }
    }

    private static void StartIpcListener()
    {
        var thread = new Thread(IpcListenerThread)
        {
            IsBackground = true,
            Name = "FastStart IPC Listener"
        };
        thread.Start();
    }

    private static void IpcListenerThread()
    {
        while (true)
        {
            try
            {
                using var server = new NamedPipeServerStream(PipeName, PipeDirection.In);
                server.WaitForConnection();

                using var reader = new System.IO.StreamReader(server);
                var message = reader.ReadLine();

                if (message == "SHOW")
                {
                    // Signal the app to show window
                    App.Current?.DispatcherQueue?.TryEnqueue(() =>
                    {
                        App.Current?.ShowMainWindow();
                    });
                }
            }
            catch (Exception)
            {
                // Pipe was broken or app is shutting down
                Thread.Sleep(100);
            }
        }
    }
}
