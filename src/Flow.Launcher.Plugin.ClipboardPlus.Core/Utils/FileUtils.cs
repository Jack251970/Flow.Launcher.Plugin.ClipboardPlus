using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Utils;

public static class FileUtils
{
    public static string SaveImageCache(ClipboardData clipboardData, string imageCachePath, string name)
    {
        if (clipboardData.Data is not BitmapSource img)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        if (!Directory.Exists(imageCachePath))
        {
            Directory.CreateDirectory(imageCachePath);
        }

        var imagePath = Path.Join(imageCachePath, $"{name}.png");
        if (File.Exists(imagePath))
        {
            File.Delete(imagePath);
        }

        try
        {
            img.Save(imagePath);
            return imagePath;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public static void ClearImageCache(string imageCachePath)
    {
        if (Directory.Exists(imageCachePath))
        {
            Directory.Delete(imageCachePath, true);
        }
    }

    public static void ClearImageCache(string imageCachePath, string name)
    {
        var imagePath = Path.Join(imageCachePath, $"{name}.png");
        if (File.Exists(imagePath))
        {
            File.Delete(imagePath);
        }
    }

    public static bool Exists(string path)
    {
        var isFile = File.Exists(path);
        var isDirectory = Directory.Exists(path);
        return isFile || isDirectory;
    }

    public static bool IsImageFile(string path)
    {
        // Check if the file path is null or empty
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        // Check if the file exists
        if (!File.Exists(path))
        {
            return false;
        }

        // Check for valid image extensions
        var validExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif", ".tiff" };
        var fileExtension = Path.GetExtension(path)?.ToLowerInvariant();

        if (fileExtension == null || Array.IndexOf(validExtensions, fileExtension) == -1)
        {
            return false;
        }

        return true;
    }
}
