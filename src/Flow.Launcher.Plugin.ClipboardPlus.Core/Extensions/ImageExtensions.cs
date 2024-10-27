using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Extensions;

public static class ImageExtensions
{
    #region System.Drawing.Image

    /*public static BitmapImage ToBitmapImage(this Image img)
    {
        var stream = new MemoryStream();
        img.Save(stream, ImageFormat.Png);
        var im = new BitmapImage();
        im.BeginInit();
        im.CacheOption = BitmapCacheOption.OnLoad;
        im.StreamSource = stream;
        im.EndInit();
        im.Freeze();
        stream.Close();
        return im;
    }

    public static string ToBase64(this Image img)
    {
        var bytes = img.ToBytes();
        return Convert.ToBase64String(bytes);
    }

    private static byte[] ToBytes(this Image img)
    {
        using var m = new MemoryStream();
        img.Save(m, ImageFormat.Png);
        return m.ToArray();
    }*/

    #endregion

    #region System.Windows.Media.Imaging.BitmapImage

    /*public static Image ToImage(this BitmapImage image)
    {
        using var m = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(image);
        encoder.Frames.Add(frame);
        encoder.Save(m);
        return Image.FromStream(m);
    }*/

    public static BitmapImage ToImage(this string filePath)
    {
        var bitmapImage = new BitmapImage();

        using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            using var memoryStream = new MemoryStream();
            fileStream.CopyTo(memoryStream);
            memoryStream.Position = 0;
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.EndInit();
            bitmapImage.Freeze();
        }

        return bitmapImage;
    }

    public static string ToBase64(this BitmapImage image)
    {
        var bytes = image.ToBytes();
        return Convert.ToBase64String(bytes);
    }

    public static void Save(this BitmapImage img, string path)
    {
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(img);
        encoder.Frames.Add(frame);
        using var stream = new FileStream(path, FileMode.Create);
        encoder.Save(stream);
    }

    private static byte[] ToBytes(this BitmapImage image)
    {
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(image);
        encoder.Frames.Add(frame);
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    #endregion

    #region System.Windows.Media.Imaging.BitmapSource

    public static BitmapImage ToBitmapImage(this BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(source);
        encoder.Frames.Add(frame);
        using var m = new MemoryStream();
        encoder.Save(m);
        m.Seek(0, SeekOrigin.Begin);
        var im = new BitmapImage();
        im.BeginInit();
        im.CacheOption = BitmapCacheOption.OnLoad;
        im.StreamSource = m;
        im.EndInit();
        im.Freeze();
        return im;
    }

    public static string ToBase64(this BitmapSource source)
    {
        var bytes = source.ToBytes();
        return Convert.ToBase64String(bytes);
    }

    public static void Save(this BitmapSource source, string path)
    {
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(source);
        encoder.Frames.Add(frame);
        using var stream = new FileStream(path, FileMode.Create);
        encoder.Save(stream);
    }

    private static byte[] ToBytes(this BitmapSource source)
    {
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(source);
        encoder.Frames.Add(frame);
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    #endregion

    #region System.String

    public static BitmapImage? ToBitmapImage(this string base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            return null;
        }
        using var m = new MemoryStream();
        var bytes = Convert.FromBase64String(base64);
        m.Write(bytes, 0, bytes.Length);
        m.Seek(0, SeekOrigin.Begin);
        var im = new BitmapImage();
        im.BeginInit();
        im.CacheOption = BitmapCacheOption.OnLoad;
        im.StreamSource = m;
        im.EndInit();
        im.Freeze();
        return im;
    }

    /*public static Image? ToImage(this string base64)
    {
        if (string.IsNullOrEmpty(base64))
        {
            return null;
        }
        var bytes = Convert.FromBase64String(base64);
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }*/

    #endregion
}
