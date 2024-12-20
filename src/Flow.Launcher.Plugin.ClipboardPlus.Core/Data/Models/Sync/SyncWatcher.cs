﻿namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class SyncWatcher : IDisposable
{
    public Func<List<SyncDataEventArgs>, Task>? SyncDataInitialized;

    public EventHandler<SyncDataEventArgs>? SyncDataChanged;

    private bool _enabled = false;
    public bool Enabled
    {
        get => _enabled;
        set
        {
            if (_enabled != value)
            {
                if (value)
                {
                    StartWatchers();
                }
                else
                {
                    StopWatchers();
                }
                _enabled = value;
            }
        }
    }

    private FileSystemWatcher _directoryWatcher = null!;

    private readonly Dictionary<string, FileSystemWatcher> _filesWatchers = new();

    public async Task InitializeWatchers(string path)
    {
        _directoryWatcher = new FileSystemWatcher
        {
            Path = path,
            Filter = "*.*",
            NotifyFilter = NotifyFilters.DirectoryName
        };
        _directoryWatcher.Created += DirectoryWatcher_OnCreated;
        _directoryWatcher.Deleted += DirectoryWatcher_OnDeleted;
        _directoryWatcher.EnableRaisingEvents = false;

        var folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        var args = new List<SyncDataEventArgs>();
        foreach (var folder in folders)
        {
            var folderName = Path.GetFileName(folder);
            if (folderName != StringUtils.EncryptKeyMd5)
            {
                AddFileWatcher(folderName, folder, false);

                args.Add(new SyncDataEventArgs
                {
                    EventType = SyncEventType.Init,
                    EncryptKeyMd5 = folderName,
                    FolderPath = folder
                });
            }
        }

        if (SyncDataInitialized != null)
        {
            await SyncDataInitialized.Invoke(args);
        }

        if (_enabled)
        {
            StartWatchers();
        }
    }

    public async Task ChangeSyncDatabasePath(string path)
    {
        if (path == _directoryWatcher.Path)
        {
            return;
        }

        RemoveWatchers();
        await InitializeWatchers(path);
    }

    private void StartWatchers()
    {
        _directoryWatcher.EnableRaisingEvents = true;
        foreach (var watcher in _filesWatchers)
        {
            watcher.Value.EnableRaisingEvents = true;
        }
    }

    private void StopWatchers()
    {
        _directoryWatcher.EnableRaisingEvents = false;
        foreach (var watcher in _filesWatchers)
        {
            watcher.Value.EnableRaisingEvents = false;
        }
    }

    private void AddFileWatcher(string folderName, string folder, bool invoke)
    {
        if (!StringUtils.IsMd5(folderName))
        {
            return;
        }

        if (folderName == StringUtils.EncryptKeyMd5)
        {
            return;
        }

        var fileWatcher = new FileSystemWatcher
        {
            Path = folder,
            Filter = PathHelper.SyncDataFile,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        fileWatcher.Changed += FileWatcher_OnChanged;
        fileWatcher.EnableRaisingEvents = false;

        _filesWatchers.Add(folderName, fileWatcher);

        if (invoke)
        {
            SyncDataChanged?.Invoke(this, new SyncDataEventArgs
            {
                EventType = SyncEventType.Add,
                EncryptKeyMd5 = folderName,
                FolderPath = folder
            });
        }
    }

    private void DirectoryWatcher_OnCreated(object sender, FileSystemEventArgs e)
    {
        var folderName = e.Name;
        var folder = e.FullPath;
        if (!string.IsNullOrEmpty(folderName))
        {
            AddFileWatcher(folderName, folder, true);
        }
    }

    private void DirectoryWatcher_OnDeleted(object sender, FileSystemEventArgs e)
    {
        var folderName = e.Name;
        var folder = e.FullPath;
        if (!string.IsNullOrEmpty(folderName))
        {
            _filesWatchers.Remove(folderName);
            SyncDataChanged?.Invoke(this, new SyncDataEventArgs
            {
                EventType = SyncEventType.Delete,
                EncryptKeyMd5 = folderName,
                FolderPath = folder
            });
        }
    }

    private void FileWatcher_OnChanged(object sender, FileSystemEventArgs e)
    {
        var file = e.FullPath;
        var folder = Path.GetDirectoryName(file);
        var folderName = Path.GetFileName(folder);
        if ((!string.IsNullOrEmpty(folder)) && (!string.IsNullOrEmpty(folderName)))
        {
            SyncDataChanged?.Invoke(this, new SyncDataEventArgs
            {
                EventType = SyncEventType.Change,
                EncryptKeyMd5 = folderName,
                FolderPath = folder
            });
        }
    }

    #region IDisposable Interface

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (disposing)
        {
            RemoveWatchers();
            _disposed = true;
        }
    }

    private void RemoveWatchers()
    {
        _directoryWatcher.Changed -= DirectoryWatcher_OnCreated;
        _directoryWatcher.Deleted -= DirectoryWatcher_OnDeleted;
        _directoryWatcher.Dispose();
        _directoryWatcher = null!;
        foreach (var watcher in _filesWatchers)
        {
            watcher.Value.Changed -= FileWatcher_OnChanged;
            watcher.Value.Dispose();
        }
        _filesWatchers.Clear();
    }

    #endregion
}

public class SyncDataEventArgs
{
    public required SyncEventType EventType { get; set; }
    public required string EncryptKeyMd5 { get; set; }
    public required string FolderPath { get; set; }
}

public enum SyncEventType
{
    Init,
    Add,
    Delete,
    Change
}
