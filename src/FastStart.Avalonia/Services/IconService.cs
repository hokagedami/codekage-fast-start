using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using FastStart.Core.Models;
using AvaBitmap = Avalonia.Media.Imaging.Bitmap;
using SysIcon = System.Drawing.Icon;
using SysImageFormat = System.Drawing.Imaging.ImageFormat;

namespace FastStart.Avalonia.Services;

public sealed class IconService
{
    private readonly ConcurrentDictionary<string, AvaBitmap?> _cache = new();

    public async Task<AvaBitmap?> GetIconAsync(AppInfo app)
    {
        var iconKey = app.IconPath ?? app.ExecutablePath;
        if (string.IsNullOrEmpty(iconKey))
            return null;

        if (_cache.TryGetValue(iconKey, out var cached))
            return cached;

        AvaBitmap? bitmap = null;
        try
        {
            byte[]? pngData = null;

            if (!string.IsNullOrEmpty(app.IconPath) && !app.IconPath.StartsWith("ms-appx", StringComparison.OrdinalIgnoreCase))
            {
                var (path, iconIndex) = ParseIconPath(app.IconPath);
                if (File.Exists(path))
                {
                    pngData = await Task.Run(() => ExtractIconToPng(path, iconIndex));
                }
            }

            if (pngData is null && !string.IsNullOrEmpty(app.ExecutablePath) && File.Exists(app.ExecutablePath))
            {
                pngData = await Task.Run(() => ExtractIconToPng(app.ExecutablePath, 0));
            }

            if (pngData is not null && pngData.Length > 0)
            {
                using var stream = new MemoryStream(pngData);
                bitmap = new AvaBitmap(stream);
            }
        }
        catch
        {
            bitmap = null;
        }

        _cache.TryAdd(iconKey, bitmap);
        return bitmap;
    }

    private static (string path, int index) ParseIconPath(string iconPath)
    {
        var lastComma = iconPath.LastIndexOf(',');
        if (lastComma > 0 && int.TryParse(iconPath.AsSpan(lastComma + 1), out var index))
        {
            return (iconPath[..lastComma], index);
        }
        return (iconPath, 0);
    }

    private static byte[]? ExtractIconToPng(string filePath, int iconIndex)
    {
        try
        {
            var hIcon = ExtractIcon(IntPtr.Zero, filePath, iconIndex);
            if (hIcon == IntPtr.Zero || hIcon == (IntPtr)1)
            {
                var shinfo = new SHFILEINFO();
                var flags = SHGFI_ICON | SHGFI_LARGEICON;
                var result = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

                if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
                    return null;

                hIcon = shinfo.hIcon;
            }

            try
            {
                using var icon = SysIcon.FromHandle(hIcon);
                using var bmp = icon.ToBitmap();
                using var stream = new MemoryStream();
                bmp.Save(stream, SysImageFormat.Png);
                return stream.ToArray();
            }
            finally
            {
                DestroyIcon(hIcon);
            }
        }
        catch
        {
            return null;
        }
    }

    #region P/Invoke

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);

    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);

    private const uint SHGFI_ICON = 0x100;
    private const uint SHGFI_LARGEICON = 0x0;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct SHFILEINFO
    {
        public IntPtr hIcon;
        public int iIcon;
        public uint dwAttributes;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string szDisplayName;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
        public string szTypeName;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbSizeFileInfo, uint uFlags);

    #endregion
}
