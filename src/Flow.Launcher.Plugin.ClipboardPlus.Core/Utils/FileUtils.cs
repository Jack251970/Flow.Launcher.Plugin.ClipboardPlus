namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Utils;

public static class FileUtils
{
    public static string SaveImageCache(ClipboardData clipboardData, string imageCachePath, string name)
    {
        if (clipboardData.Data is not Image img)
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

        var path = Path.Join(imageCachePath, $"{name}.png");
        img.Save(path);
        return path;
    }

    public static void ClearImageCache(string imageCachePath)
    {
        if (Directory.Exists(imageCachePath))
        {
            Directory.Delete(imageCachePath, true);
        }
    }
}
