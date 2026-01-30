#r "nuget: System.Drawing.Common, 8.0.0"

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// Generate FastStart icon - lightning bolt on blue background
void GenerateIcon(string outputPath)
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

            float scale = size / 256f;

            // Background rounded rect
            using (var bgBrush = new LinearGradientBrush(
                new Rectangle(0, 0, size, size),
                Color.FromArgb(0, 120, 212),
                Color.FromArgb(0, 90, 158),
                45f))
            {
                var radius = 48 * scale;
                var bgPath = RoundedRect(new RectangleF(8 * scale, 8 * scale, 240 * scale, 240 * scale), radius);
                g.FillPath(bgBrush, bgPath);
            }

            // Lightning bolt
            var boltPoints = new PointF[]
            {
                new PointF(148 * scale, 40 * scale),
                new PointF(88 * scale, 128 * scale),
                new PointF(120 * scale, 128 * scale),
                new PointF(108 * scale, 216 * scale),
                new PointF(168 * scale, 120 * scale),
                new PointF(132 * scale, 120 * scale),
            };

            using (var boltBrush = new LinearGradientBrush(
                new Rectangle(0, 0, size, size),
                Color.FromArgb(255, 215, 0),
                Color.FromArgb(255, 165, 0),
                45f))
            using (var boltPen = new Pen(Color.White, 4 * scale))
            {
                g.FillPolygon(boltBrush, boltPoints);
                g.DrawPolygon(boltPen, boltPoints);
            }

            // Speed lines
            using (var linePen = new Pen(Color.FromArgb(180, 255, 255, 255), 8 * scale))
            {
                linePen.StartCap = LineCap.Round;
                linePen.EndCap = LineCap.Round;
                g.DrawLine(linePen, 40 * scale, 100 * scale, 72 * scale, 100 * scale);
                linePen.Color = Color.FromArgb(220, 255, 255, 255);
                g.DrawLine(linePen, 48 * scale, 128 * scale, 80 * scale, 128 * scale);
                linePen.Color = Color.FromArgb(180, 255, 255, 255);
                g.DrawLine(linePen, 40 * scale, 156 * scale, 72 * scale, 156 * scale);
            }
        }
        images.Add(bmp);
    }

    // Save as ICO
    SaveAsIcon(images, outputPath);
    Console.WriteLine($"Icon saved to: {outputPath}");

    foreach (var img in images) img.Dispose();
}

GraphicsPath RoundedRect(RectangleF bounds, float radius)
{
    var path = new GraphicsPath();
    path.AddArc(bounds.X, bounds.Y, radius * 2, radius * 2, 180, 90);
    path.AddArc(bounds.Right - radius * 2, bounds.Y, radius * 2, radius * 2, 270, 90);
    path.AddArc(bounds.Right - radius * 2, bounds.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
    path.AddArc(bounds.X, bounds.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
    path.CloseFigure();
    return path;
}

void SaveAsIcon(List<Image> images, string path)
{
    using var fs = new FileStream(path, FileMode.Create);
    using var bw = new BinaryWriter(fs);

    // ICO header
    bw.Write((short)0);           // Reserved
    bw.Write((short)1);           // Type: 1 = ICO
    bw.Write((short)images.Count); // Number of images

    var imageDataList = new List<byte[]>();
    int offset = 6 + (16 * images.Count); // Header + directory entries

    // Write directory entries
    foreach (var img in images)
    {
        using var ms = new MemoryStream();
        img.Save(ms, ImageFormat.Png);
        var data = ms.ToArray();
        imageDataList.Add(data);

        bw.Write((byte)(img.Width >= 256 ? 0 : img.Width));
        bw.Write((byte)(img.Height >= 256 ? 0 : img.Height));
        bw.Write((byte)0);        // Color palette
        bw.Write((byte)0);        // Reserved
        bw.Write((short)1);       // Color planes
        bw.Write((short)32);      // Bits per pixel
        bw.Write(data.Length);    // Size of image data
        bw.Write(offset);         // Offset to image data
        offset += data.Length;
    }

    // Write image data
    foreach (var data in imageDataList)
    {
        bw.Write(data);
    }
}

GenerateIcon(Args[0]);
