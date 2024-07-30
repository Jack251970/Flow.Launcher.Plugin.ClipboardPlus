namespace ClipboardPlus.Core.Utils;

public static class FileUtils
{
    public static string? SaveImageCache(ClipboardData clipboardData, DirectoryInfo clipCacheDir, string? name = null)
    {
        if (clipboardData.Data is not Image img)
        {
            return null;
        }

        name = string.IsNullOrWhiteSpace(name) ? StringUtils.RandomString(10) : name;
        var path = Path.Join(clipCacheDir.FullName, $"{name}.png");

        img.Save(path);
        return path;
    }

    public static (DirectoryInfo ClipDir, DirectoryInfo ClipCacheDir) GetClipDirAndClipCacheDir(PluginInitContext context)
    {
        var clipDir = new DirectoryInfo(context.CurrentPluginMetadata.PluginDirectory);
        var imageCacheDirectoryPath = Path.Combine(clipDir.FullName, "CachedImages");
        var clipCacheDir = !Directory.Exists(imageCacheDirectoryPath)
            ? Directory.CreateDirectory(imageCacheDirectoryPath)
            : new DirectoryInfo(imageCacheDirectoryPath);

        return (clipDir, clipCacheDir);
    }
}
