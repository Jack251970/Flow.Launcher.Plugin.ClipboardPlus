using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace ClipboardPlus.Core.Extensions;

public static class ImageExtensions
{
    public static BitmapImage ToBitmapImage(this Image img)
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
        using var m = new MemoryStream();
        img.Save(m, ImageFormat.Png);
        return Convert.ToBase64String(m.ToArray());
    }

    public static string ToBase64(this BitmapImage image)
    {
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(image);
        encoder.Frames.Add(frame);
        using var stream = new MemoryStream();
        encoder.Save(stream);
        return Convert.ToBase64String(stream.ToArray());
    }

    public static BitmapImage ToBitmapImage(this string b64)
    {
        return b64.ToImage().ToBitmapImage();
    }

    public static Image ToImage(this string b64)
    {
        byte[] bytes = Convert.FromBase64String(b64);
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }
}
