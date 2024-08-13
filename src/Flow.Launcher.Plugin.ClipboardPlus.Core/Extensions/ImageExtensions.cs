using System.Text;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Extensions;

public static class ImageExtensions
{
    #region System.Drawing.Image

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

    public static string ToString(this Image img, StringType type = StringType.Default)
    {
        var bytes = img.ToBytes();
        return type switch
        {
            StringType.Base64 => Convert.ToBase64String(bytes),
            _ => Encoding.UTF8.GetString(bytes)
        };
    }

    private static byte[] ToBytes(this Image img)
    {
        using var m = new MemoryStream();
        img.Save(m, ImageFormat.Png);
        return m.ToArray();
    }

    #endregion

    #region System.Windows.Media.Imaging.BitmapImage

    public static Image ToImage(this BitmapImage image)
    {
        using var m = new MemoryStream();
        var encoder = new PngBitmapEncoder();
        var frame = BitmapFrame.Create(image);
        encoder.Frames.Add(frame);
        encoder.Save(m);
        return Image.FromStream(m);
    }

    public static string ToString(this BitmapImage image, StringType type = StringType.Default)
    {
        var bytes = image.ToBytes();
        return type switch
        {
            StringType.Base64 => Convert.ToBase64String(bytes),
            _ => Encoding.UTF8.GetString(bytes)
        };
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

    #region System.String

    public static BitmapImage ToBitmapImage(this string base64, StringType type = StringType.Default)
    {
        using var m = new MemoryStream();
        byte[] bytes = type switch
        {
            StringType.Base64 => Convert.FromBase64String(base64),
            _ => Encoding.UTF8.GetBytes(base64)
        };
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

    public static Image ToImage(this string base64, StringType type = StringType.Default)
    {
        byte[] bytes = type switch
        {
            StringType.Base64 => Convert.FromBase64String(base64),
            _ => Encoding.UTF8.GetBytes(base64)
        };
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }

    #endregion
}
