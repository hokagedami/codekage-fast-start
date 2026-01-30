using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace IconGenerator;

public static class Program
{
    public static void Main(string[] args)
    {
        var outputPath = args.Length > 0
            ? args[0]
            : Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "assets", "FastStart.ico");

        outputPath = Path.GetFullPath(outputPath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        GenerateIcon(outputPath);
    }

    private static void GenerateIcon(string outputPath)
    {
        var sizes = new[] { 16, 32, 48, 64, 128, 256 };
        var images = new List<Image>();

        foreach (var size in sizes)
        {
            var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float scale = size / 256f;

                // Background rounded rect with gradient
                using var bgBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, size, size),
                    Color.FromArgb(0, 120, 212),  // #0078D4
                    Color.FromArgb(0, 90, 158),   // #005A9E
                    45f);

                var radius = 48 * scale;
                using var bgPath = RoundedRect(
                    new RectangleF(8 * scale, 8 * scale, 240 * scale, 240 * scale),
                    radius);
                g.FillPath(bgBrush, bgPath);

                // Lightning bolt
                var boltPoints = new PointF[]
                {
                    new(148 * scale, 40 * scale),
                    new(88 * scale, 128 * scale),
                    new(120 * scale, 128 * scale),
                    new(108 * scale, 216 * scale),
                    new(168 * scale, 120 * scale),
                    new(132 * scale, 120 * scale),
                };

                using var boltBrush = new LinearGradientBrush(
                    new Rectangle(0, 0, size, size),
                    Color.FromArgb(255, 215, 0),   // Gold
                    Color.FromArgb(255, 165, 0),   // Orange
                    45f);
                using var boltPen = new Pen(Color.White, 4 * scale);

                g.FillPolygon(boltBrush, boltPoints);
                g.DrawPolygon(boltPen, boltPoints);

                // Speed lines
                using var linePen = new Pen(Color.FromArgb(180, 255, 255, 255), 8 * scale);
                linePen.StartCap = LineCap.Round;
                linePen.EndCap = LineCap.Round;

                g.DrawLine(linePen, 40 * scale, 100 * scale, 72 * scale, 100 * scale);
                linePen.Color = Color.FromArgb(220, 255, 255, 255);
                g.DrawLine(linePen, 48 * scale, 128 * scale, 80 * scale, 128 * scale);
                linePen.Color = Color.FromArgb(180, 255, 255, 255);
                g.DrawLine(linePen, 40 * scale, 156 * scale, 72 * scale, 156 * scale);
            }
            images.Add(bmp);
        }

        SaveAsIcon(images, outputPath);
        Console.WriteLine($"Icon saved to: {outputPath}");

        foreach (var img in images)
            img.Dispose();
    }

    private static GraphicsPath RoundedRect(RectangleF bounds, float radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;

        path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();

        return path;
    }

    private static void SaveAsIcon(List<Image> images, string path)
    {
        using var fs = new FileStream(path, FileMode.Create);
        using var bw = new BinaryWriter(fs);

        // ICO header
        bw.Write((short)0);            // Reserved
        bw.Write((short)1);            // Type: 1 = ICO
        bw.Write((short)images.Count); // Number of images

        var imageDataList = new List<byte[]>();
        var offset = 6 + (16 * images.Count); // Header + directory entries

        // Write directory entries
        foreach (var img in images)
        {
            using var ms = new MemoryStream();
            img.Save(ms, ImageFormat.Png);
            var data = ms.ToArray();
            imageDataList.Add(data);

            bw.Write((byte)(img.Width >= 256 ? 0 : img.Width));
            bw.Write((byte)(img.Height >= 256 ? 0 : img.Height));
            bw.Write((byte)0);         // Color palette
            bw.Write((byte)0);         // Reserved
            bw.Write((short)1);        // Color planes
            bw.Write((short)32);       // Bits per pixel
            bw.Write(data.Length);     // Size of image data
            bw.Write(offset);          // Offset to image data
            offset += data.Length;
        }

        // Write image data
        foreach (var data in imageDataList)
        {
            bw.Write(data);
        }
    }
}
