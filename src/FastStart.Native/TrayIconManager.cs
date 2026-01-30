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

    private NOTIFYICONDATA _nid;
    private bool _isVisible;
    private bool _disposed;

    public IntPtr WindowHandle { get; set; }

    public void Show(string tooltip = "FastStart")
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

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(int dwMessage, ref NOTIFYICONDATA lpData);

    [DllImport("user32.dll")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);
}
