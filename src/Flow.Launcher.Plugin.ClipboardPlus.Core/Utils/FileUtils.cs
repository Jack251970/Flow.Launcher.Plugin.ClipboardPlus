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

    public static string GetSaveJsonFile(IClipboardPlus clipboardPlus)
    {
        var saveFileDialog = new SaveFileDialog
        {
            Filter = GetJsonFileFilter(clipboardPlus),
            DefaultExt = "json",
            AddExtension = true
        };

        if (saveFileDialog.ShowDialog() != DialogResult.OK)
        {
            return string.Empty;
        }

        return saveFileDialog.FileName;
    }

    public static string GetOpenJsonFile(IClipboardPlus clipboardPlus)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = GetJsonFileFilter(clipboardPlus)
        };

        if (openFileDialog.ShowDialog() != DialogResult.OK)
        {
            return string.Empty;
        }

        return openFileDialog.FileName;
    }

    public static string GetSyncDatabaseFolder()
    {
        var folderBrowserDialog = new FolderBrowserDialog();

        if (folderBrowserDialog.ShowDialog() != DialogResult.OK)
        {
            return string.Empty;
        }

        return folderBrowserDialog.SelectedPath;
    }

    private static string GetJsonFileFilter(IClipboardPlus clipboardPlus)
    {
        var context = clipboardPlus.Context;
        if (context == null)
        {
            return "Json files (*.json)|*.json|All files (*.*)|*.*";
        }
        else
        {
            return $"{context.GetTranslation("flowlauncher_plugin_clipboardplus_json_files")} (*.json)|*.json|" +
                $"{context.GetTranslation("flowlauncher_plugin_clipboardplus_json_files")} (*.*)|*.*";
        }
    }

    public static void CopyFilesFromOneFolderToAnother(string oldFolder, string newFolder)
    {
        // create new folder if it doesn't exist
        if (!Directory.Exists(newFolder))
        {
            Directory.CreateDirectory(newFolder);
        }

        // copy files from old folder to new folder
        var oldFolderLength = oldFolder.Length;
        var files = Directory.GetFiles(oldFolder, "*.*", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            var destFile = Path.Combine(newFolder, file[(oldFolderLength + 1)..]);
            var directory = Path.GetDirectoryName(destFile);
            if ((!string.IsNullOrEmpty(directory)) && (!Directory.Exists(directory)))
            {
                Directory.CreateDirectory(directory);
            }
            File.Copy(file, destFile, true);
        }

        // delete all folders and files under older folder and keep old folder
        var directories = Directory.GetDirectories(oldFolder, "*", SearchOption.AllDirectories);
        foreach (var directory in directories)
        {
            Directory.Delete(directory, true);
        }
        var oldFiles = Directory.GetFiles(oldFolder, "*.*", SearchOption.AllDirectories);
        foreach (var file in oldFiles)
        {
            File.Delete(file);
        }
    }
}
