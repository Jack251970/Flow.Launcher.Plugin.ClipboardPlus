namespace ClipboardPlus.Core.Utils;

public static class FileUtils
{
    public static string? SaveImageCache(ClipboardData clipboardData, string clipCacheDir, string? name = null)
    {
        if (clipboardData.Data is not Image img)
        {
            return null;
        }

        name = string.IsNullOrWhiteSpace(name) ? StringUtils.RandomString(10) : name;
        var path = Path.Join(clipCacheDir, $"{name}.png");

        img.Save(path);
        return path;
    }
}
