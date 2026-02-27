using Flow.Launcher.Plugin.ClipboardPlus.Core.Data.AppInfo;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Com;
using Windows.Win32.UI.Shell;
using Path = System.IO.Path;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public static class ShortcutHelper
{
    private static readonly string[] startMenuPaths =
    [
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartMenu), "Programs"),
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs")
    ];

    private static IReadOnlyList<FileSystemWatcher> fileWatchers = new List<FileSystemWatcher>();
    private static Task<List<ShortcutAppInfo>>? startMenuAppsListTask;

    static ShortcutHelper()
    {
        // Subscribe to file system changes to invalidate cache
        try
        {
            var watchers = new List<FileSystemWatcher>();
            foreach (string path in startMenuPaths)
            {
                if (Directory.Exists(path))
                {
                    var watcher = new FileSystemWatcher(path)
                    {
                        IncludeSubdirectories = true,
                        NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.LastWrite
                    };

                    watcher.Created += OnStartMenuChanged;
                    watcher.Deleted += OnStartMenuChanged;
                    watcher.Changed += OnStartMenuChanged;
                    watcher.Renamed += OnStartMenuChanged;

                    watcher.EnableRaisingEvents = true;
                    watchers.Add(watcher);
                }
            }

            fileWatchers = watchers;
        }
        catch (Exception)
        {
            // Error while subscribing to Start Menu file system events
        }
    }

    public static async Task<List<ShortcutAppInfo>> GetStartMenuAppsAsync()
    {
        startMenuAppsListTask ??= LoadStartMenuAppsAsync();
        return await startMenuAppsListTask;
    }

    internal static ShortcutAppInfo? GetShortcutAppInfo(string lnkPath)
    {
        try
        {
            ShortcutInfo? shortcutInfo = GetShortcutInfo(lnkPath);
            if (shortcutInfo != null && !string.IsNullOrEmpty(shortcutInfo.TargetPath))
            {
                var startMenuAppInfo = new ShortcutAppInfo
                {
                    DefaultDisplayName = shortcutInfo.Title,
                    DisplayName = shortcutInfo.Title,
                    TargetPath = shortcutInfo.TargetPath,
                    IconPath = shortcutInfo.IconPath,
                    ShortcutFilePath = lnkPath
                };
                startMenuAppInfo.OnDeserialized();
                return startMenuAppInfo;
            }
        }
        catch (Exception)
        {
            // Error while parsing shortcut: {Path}", lnkPath
        }

        return null;
    }

    private static void OnStartMenuChanged(object sender, FileSystemEventArgs e)
    {
        if (e.FullPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
        {
            InvalidateCache();
        }
    }

    private static void InvalidateCache()
    {
        if (startMenuAppsListTask is not null)
        {
            // Start Menu changed, invalidating Start Menu apps cache
            startMenuAppsListTask = LoadStartMenuAppsAsync();
        }
    }

    private static Task<List<ShortcutAppInfo>> LoadStartMenuAppsAsync()
    {
        return Task.Run(() =>
        {
            List<ShortcutAppInfo> startMenuApps = [];

            try
            {
                foreach (string startMenuPath in startMenuPaths)
                {
                    if (!Directory.Exists(startMenuPath))
                    {
                        continue;
                    }

                    string[] lnkFiles = Directory.GetFiles(startMenuPath, "*.lnk", SearchOption.AllDirectories);

                    foreach (string lnkPath in lnkFiles)
                    {
                        ShortcutAppInfo? startMenuAppInfo = GetShortcutAppInfo(lnkPath);
                        if (startMenuAppInfo is not null)
                        {
                            startMenuApps.Add(startMenuAppInfo);
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Error while getting Start Menu apps
            }

            return startMenuApps;
        });
    }

    private static ShortcutInfo? GetShortcutInfo(string lnkPath)
    {
        try
        {
            var shell = new ShellLink();
            var link = (IShellLinkW)shell;
            var file = (IPersistFile)shell;

            file.Load(lnkPath, 0);

            const int MAX_PATH = 260;

            // Allocate unmanaged buffers
            nint targetPathPtr = Marshal.AllocHGlobal(MAX_PATH * 2);
            nint argumentsPtr = Marshal.AllocHGlobal(MAX_PATH * 2);
            nint workingDirectoryPtr = Marshal.AllocHGlobal(MAX_PATH * 2);
            nint iconPathPtr = Marshal.AllocHGlobal(MAX_PATH * 2);

            try
            {
                unsafe
                {
                    link.GetPath(new PWSTR((char*)targetPathPtr), MAX_PATH, null, 0);
                    link.GetArguments(new PWSTR((char*)argumentsPtr), MAX_PATH);
                    link.GetWorkingDirectory(new PWSTR((char*)workingDirectoryPtr), MAX_PATH);
                    link.GetIconLocation(new PWSTR((char*)iconPathPtr), MAX_PATH, out int iconIndex);

                    string targetPath = Marshal.PtrToStringUni(targetPathPtr) ?? string.Empty;
                    string arguments = Marshal.PtrToStringUni(argumentsPtr) ?? string.Empty;
                    string workingDirectory = Marshal.PtrToStringUni(workingDirectoryPtr) ?? string.Empty;
                    string iconPath = Marshal.PtrToStringUni(iconPathPtr) ?? string.Empty;
                    string title = Path.GetFileNameWithoutExtension(lnkPath);

                    string normalizedTargetPath = NormalizePath(targetPath) ?? string.Empty;
                    string normalizedIconPath = NormalizePath(iconPath) ?? string.Empty;

                    if (string.IsNullOrEmpty(normalizedTargetPath) && string.IsNullOrEmpty(normalizedIconPath))
                    {
                        // Invalid target path. The shortcut likely points to a non-existent file.
                        return null;
                    }

                    return new ShortcutInfo
                    {
                        Title = title,
                        TargetPath = normalizedTargetPath ?? string.Empty,
                        IconPath = normalizedIconPath ?? string.Empty,
                    };
                }
            }
            finally
            {
                Marshal.FreeHGlobal(targetPathPtr);
                Marshal.FreeHGlobal(argumentsPtr);
                Marshal.FreeHGlobal(workingDirectoryPtr);
                Marshal.FreeHGlobal(iconPathPtr);
            }
        }
        catch (Exception)
        {
            // Failed to read shortcut: {Path}", lnkPath
            return null;
        }
    }

    private static string? NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        // Expand environment variables (e.g., %WINDIR%, %TEMP%)
        string expandedPath = Environment.ExpandEnvironmentVariables(path);

        // Normalize the path to handle various formats
        expandedPath = Path.GetFullPath(expandedPath);

        if (!File.Exists(expandedPath))
        {
            return null;
        }

        // StorageFile.GetFileFromPathAsync has restrictions:
        // - Doesn't work with UNC paths (\\server\share)
        // - Doesn't work with some system paths
        // - Requires proper path format
        // Check if path is a local, absolute path that StorageFile can handle
        if (!IsPathSupportedByStorageFile(expandedPath))
        {
            return null;
        }

        return expandedPath;
    }

    private static bool IsPathSupportedByStorageFile(string path)
    {
        // UNC paths are not supported
        if (path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return false;
        }

        // Must be a rooted path (absolute)
        if (!Path.IsPathRooted(path))
        {
            return false;
        }

        // Check if it's a valid local path (has a drive letter)
        if (path.Length < 2 || path[1] != ':')
        {
            return false;
        }

        return true;
    }

    private sealed class ShortcutInfo
    {
        public required string Title { get; init; }
        public required string TargetPath { get; init; }
        public required string IconPath { get; init; }
    }
}
