namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Utils;

public static class FileUtils
{
    public static string SaveImageCache(ClipboardData clipboardData, string clipCacheDir, string name)
    {
        if (clipboardData.Data is not Image img)
        {
            return string.Empty;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var path = Path.Join(clipCacheDir, $"{name}.png");
        img.Save(path);
        return path;
    }
}
