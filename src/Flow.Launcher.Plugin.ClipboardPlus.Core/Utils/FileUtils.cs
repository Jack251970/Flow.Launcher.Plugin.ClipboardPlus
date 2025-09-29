using Microsoft.Win32;
using System;
using System.IO;
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
            try
            {
                Directory.CreateDirectory(imageCachePath);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        var imagePath = Path.Join(imageCachePath, $"{name}.png");
        if (File.Exists(imagePath))
        {
            try
            {
                File.Delete(imagePath);
            }
            catch (Exception)
            {
                return string.Empty;
            }
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

    public static bool ClearImageCache(string imageCachePath)
    {
        if (Directory.Exists(imageCachePath))
        {
            try
            {
                Directory.Delete(imageCachePath, true);
            }
            catch (Exception)
            {
                return false;
            }
        }
        return true;
    }

    public static bool ClearImageCache(string imageCachePath, string name)
    {
        var imagePath = Path.Join(imageCachePath, $"{name}.png");
        if (File.Exists(imagePath))
        {
            try
            {
                File.Delete(imagePath);
            }
            catch (Exception)
            {
                return false;
            }
        }
        return true;
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

        if (saveFileDialog.ShowDialog() != true)
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

        if (openFileDialog.ShowDialog() != true)
        {
            return string.Empty;
        }

        return openFileDialog.FileName;
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
            return $"{Localize.flowlauncher_plugin_clipboardplus_json_files()} (*.json)|*.json|" +
                $"{Localize.flowlauncher_plugin_clipboardplus_json_files()} (*.*)|*.*";
        }
    }

    public static bool MoveFile(string sourcePath, string destinationPath)
    {
        if (!File.Exists(sourcePath))
        {
            return false;
        }

        if (File.Exists(destinationPath))
        {
            try
            {
                File.Delete(sourcePath);
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        var destinationDirectory = Path.GetDirectoryName(destinationPath);
        try
        {
            if (!Directory.Exists(destinationDirectory) && (!string.IsNullOrEmpty(destinationDirectory)))
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            File.Move(sourcePath, destinationPath);
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }

    public static bool MoveDirectory(string sourcePath, string destinationPath)
    {
        if (!Directory.Exists(sourcePath))
        {
            return false;
        }

        if (Directory.Exists(destinationPath))
        {
            try
            {
                Directory.Delete(sourcePath, true);
            }
            catch (Exception)
            {
                return false;
            }
            return false;
        }

        try
        {
            Directory.Move(sourcePath, destinationPath);
        }
        catch (Exception)
        {
            return false;
        }
        return true;
    }
}
