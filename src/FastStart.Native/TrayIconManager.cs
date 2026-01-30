using System.Runtime.InteropServices;

namespace FastStart.Native;

/// <summary>
/// Manages system tray icon using Shell_NotifyIcon.
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private const int NIF_MESSAGE = 0x01;
    private const int NIF_ICON = 0x02;
    private const int NIF_TIP = 0x04;
    private const int NIM_ADD = 0x00;
    private const int NIM_MODIFY = 0x01;
    private const int NIM_DELETE = 0x02;
    private const int WM_USER = 0x0400;

    public const int WM_TRAYICON = WM_USER + 1;
    public const int WM_LBUTTONUP = 0x0202;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_LBUTTONDBLCLK = 0x0203;

    // Context menu constants
    private const int MF_STRING = 0x00;
    private const int MF_SEPARATOR = 0x800;
    private const int TPM_RETURNCMD = 0x100;
    private const int TPM_NONOTIFY = 0x80;

    public const int MENU_SHOW = 1;
    public const int MENU_EXIT = 2;

    private NOTIFYICONDATA _nid;
    private bool _isVisible;
    private bool _disposed;

    public IntPtr WindowHandle { get; set; }

    /// <summary>
    /// Event raised when the tray icon is clicked.
    /// </summary>
    public event EventHandler? TrayIconClicked;

    /// <summary>
    /// Event raised when Show is selected from context menu.
    /// </summary>
    public event EventHandler? ShowRequested;

    /// <summary>
    /// Event raised when Exit is selected from context menu.
    /// </summary>
    public event EventHandler? ExitRequested;

    public void Show(string tooltip = "FastStart - Press Win to open")
    {
        if (_isVisible || WindowHandle == IntPtr.Zero) return;

        _nid = new NOTIFYICONDATA
        {
            cbSize = Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = WindowHandle,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = LoadIcon(IntPtr.Zero, (IntPtr)32512), // IDI_APPLICATION
            szTip = tooltip
        };

        Shell_NotifyIcon(NIM_ADD, ref _nid);
        _isVisible = true;
    }

    public void Hide()
    {
        if (!_isVisible) return;

        Shell_NotifyIcon(NIM_DELETE, ref _nid);
        _isVisible = false;
    }

    public void UpdateTooltip(string tooltip)
    {
        if (!_isVisible) return;

        _nid.szTip = tooltip;
        _nid.uFlags = NIF_TIP;
        Shell_NotifyIcon(NIM_MODIFY, ref _nid);
    }

    /// <summary>
    /// Handles tray icon messages. Call from window procedure.
    /// </summary>
    public void HandleTrayMessage(int lParam)
    {
        switch (lParam)
        {
            case WM_LBUTTONUP:
            case WM_LBUTTONDBLCLK:
                TrayIconClicked?.Invoke(this, EventArgs.Empty);
                ShowRequested?.Invoke(this, EventArgs.Empty);
                break;

            case WM_RBUTTONUP:
                ShowContextMenu();
                break;
        }
    }

    private void ShowContextMenu()
    {
        if (WindowHandle == IntPtr.Zero) return;

        var hMenu = CreatePopupMenu();
        if (hMenu == IntPtr.Zero) return;

        try
        {
            AppendMenu(hMenu, MF_STRING, MENU_SHOW, "Show FastStart");
            AppendMenu(hMenu, MF_SEPARATOR, 0, null);
            AppendMenu(hMenu, MF_STRING, MENU_EXIT, "Exit");

            // Get cursor position
            GetCursorPos(out var pt);

            // Required to make menu work correctly
            SetForegroundWindow(WindowHandle);

            var cmd = TrackPopupMenu(hMenu, TPM_RETURNCMD | TPM_NONOTIFY,
                pt.X, pt.Y, 0, WindowHandle, IntPtr.Zero);

            switch (cmd)
            {
                case MENU_SHOW:
                    ShowRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case MENU_EXIT:
                    ExitRequested?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
        finally
        {
            DestroyMenu(hMenu);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        Hide();
        _disposed = true;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public int cbSize;
        public IntPtr hWnd;
        public int uID;
        public int uFlags;
        public int uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll")]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern bool AppendMenu(IntPtr hMenu, int uFlags, int uIDNewItem, string? lpNewItem);

    [DllImport("user32.dll")]
    private static extern bool DestroyMenu(IntPtr hMenu);

    [DllImport("user32.dll")]
    private static extern int TrackPopupMenu(IntPtr hMenu, int uFlags, int x, int y,
        int nReserved, IntPtr hWnd, IntPtr prcRect);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
