using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;

namespace FastStart.Native.Com;

/// <summary>
/// COM interface for shell link operations.
/// </summary>
[ComImport]
[Guid("000214F9-0000-0000-C000-000000000046")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal interface IShellLinkW
{
    void GetPath(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
        int cch,
        IntPtr pfd,
        uint fFlags);

    void GetIDList(out IntPtr ppidl);

    void SetIDList(IntPtr pidl);

    void GetDescription(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
        int cch);

    void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);

    void GetWorkingDirectory(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir,
        int cch);

    void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);

    void GetArguments(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs,
        int cch);

    void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

    void GetHotkey(out ushort pwHotkey);

    void SetHotkey(ushort wHotkey);

    void GetShowCmd(out int piShowCmd);

    void SetShowCmd(int iShowCmd);

    void GetIconLocation(
        [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
        int cch,
        out int piIcon);

    void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);

    void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);

    void Resolve(IntPtr hwnd, uint fFlags);

    void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
}

/// <summary>
/// ShellLink COM class.
/// </summary>
[ComImport]
[Guid("00021401-0000-0000-C000-000000000046")]
internal class ShellLink
{
}

/// <summary>
/// Shell link resolution flags.
/// </summary>
internal static class ShellLinkResolveFlags
{
    public const uint SLR_NO_UI = 0x0001;
    public const uint SLR_ANY_MATCH = 0x0002;
    public const uint SLR_UPDATE = 0x0004;
    public const uint SLR_NOUPDATE = 0x0008;
    public const uint SLR_NOSEARCH = 0x0010;
    public const uint SLR_NOTRACK = 0x0020;
    public const uint SLR_NOLINKINFO = 0x0040;
    public const uint SLR_INVOKE_MSI = 0x0080;
    public const uint SLR_NO_UI_WITH_MSG_PUMP = 0x0101;
}

/// <summary>
/// Shell link path flags.
/// </summary>
internal static class ShellLinkPathFlags
{
    public const uint SLGP_SHORTPATH = 0x0001;
    public const uint SLGP_UNCPRIORITY = 0x0002;
    public const uint SLGP_RAWPATH = 0x0004;
}
