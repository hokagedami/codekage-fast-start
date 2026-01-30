using System.Diagnostics;
using System.Runtime.InteropServices;

namespace FastStart.Native;

/// <summary>
/// Low-level keyboard hook to intercept the Windows key.
/// </summary>
public sealed class GlobalKeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    private readonly LowLevelKeyboardProc _proc;
    private IntPtr _hookId = IntPtr.Zero;
    private bool _winKeyPressed;
    private bool _otherKeyPressed;
    private bool _disposed;

    public event EventHandler? WinKeyPressed;
    public bool IsEnabled { get; set; } = true;

    public GlobalKeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Start()
    {
        if (_hookId != IntPtr.Zero) return;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule?.ModuleName is not null)
        {
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    public void Stop()
    {
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && IsEnabled)
        {
            var vkCode = Marshal.ReadInt32(lParam);
            var isWinKey = vkCode == VK_LWIN || vkCode == VK_RWIN;

            if (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
            {
                if (isWinKey)
                {
                    _winKeyPressed = true;
                    _otherKeyPressed = false;
                }
                else if (_winKeyPressed)
                {
                    // Another key pressed while Win is held (e.g., Win+R, Win+E)
                    _otherKeyPressed = true;
                }
            }
            else if (wParam == WM_KEYUP || wParam == WM_SYSKEYUP)
            {
                if (isWinKey)
                {
                    // Only trigger if Win was pressed alone (no other keys)
                    if (_winKeyPressed && !_otherKeyPressed)
                    {
                        WinKeyPressed?.Invoke(this, EventArgs.Empty);
                        _winKeyPressed = false;
                        // Suppress the key to prevent default Start Menu
                        return (IntPtr)1;
                    }
                    _winKeyPressed = false;
                    _otherKeyPressed = false;
                }
            }

            // Suppress Win key down to prevent default Start Menu
            if (isWinKey && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) && !_otherKeyPressed)
            {
                return (IntPtr)1;
            }
        }

        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}
