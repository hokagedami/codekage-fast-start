using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using FastStart.Core.Models;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage.Streams;

namespace FastStart.UI.Services;

public sealed class IconService
{
    private readonly ConcurrentDictionary<string, ImageSource?> _cache = new();

    public async Task<ImageSource?> GetIconAsync(AppInfo app)
    {
        var iconKey = app.IconPath ?? app.ExecutablePath;
        if (string.IsNullOrEmpty(iconKey))
            return null;

        if (_cache.TryGetValue(iconKey, out var cached))
            return cached;

        ImageSource? imageSource = null;
        try
        {
            byte[]? pngData = null;

            if (!string.IsNullOrEmpty(app.IconPath))
            {
                // Handle UWP app icons (ms-appx:// URIs)
                if (app.IconPath.StartsWith("ms-appx", StringComparison.OrdinalIgnoreCase))
                {
                    imageSource = new BitmapImage(new Uri(app.IconPath));
                }
                else
                {
                    // Parse icon path (may include index like "path.exe,0")
                    var (path, iconIndex) = ParseIconPath(app.IconPath);
                    if (File.Exists(path))
                    {
                        pngData = await Task.Run(() => ExtractIconToPng(path, iconIndex));
                    }
                }
            }

            // Fallback to executable path if no icon found
            if (pngData is null && imageSource is null && !string.IsNullOrEmpty(app.ExecutablePath) && File.Exists(app.ExecutablePath))
            {
                pngData = await Task.Run(() => ExtractIconToPng(app.ExecutablePath, 0));
            }

            // Convert PNG bytes to BitmapImage
            if (pngData is not null && pngData.Length > 0)
            {
                var bitmapImage = new BitmapImage();
                using var stream = new InMemoryRandomAccessStream();
                await stream.WriteAsync(pngData.AsBuffer());
                stream.Seek(0);
                await bitmapImage.SetSourceAsync(stream);
                imageSource = bitmapImage;
            }
        }
        catch (Exception)
        {
            imageSource = null;
        }

        _cache.TryAdd(iconKey, imageSource);
        return imageSource;
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
            // Try to extract icon using Shell API
            var hIcon = ExtractIcon(IntPtr.Zero, filePath, iconIndex);
            if (hIcon == IntPtr.Zero || hIcon == (IntPtr)1)
            {
                // Fallback: try SHGetFileInfo for associated icon
                var shinfo = new SHFILEINFO();
                var flags = SHGFI_ICON | SHGFI_LARGEICON;
                var result = SHGetFileInfo(filePath, 0, ref shinfo, (uint)Marshal.SizeOf(shinfo), flags);

                if (result == IntPtr.Zero || shinfo.hIcon == IntPtr.Zero)
                    return null;

                hIcon = shinfo.hIcon;
            }

            try
            {
                using var icon = Icon.FromHandle(hIcon);
                using var bitmap = icon.ToBitmap();
                using var stream = new MemoryStream();
                bitmap.Save(stream, ImageFormat.Png);
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
