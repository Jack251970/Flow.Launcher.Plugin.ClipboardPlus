namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class SyncWatcher : IDisposable
{
    public EventHandler<SyncDataEventArgs>? SyncDataChanged;

    private bool _enabled = true;
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

    public void InitializeWatchers(string path)
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
        foreach (var folder in folders)
        {
            var folderName = Path.GetFileName(folder);
            AddFileWatcher(folderName, folder);
        }

        if (_enabled)
        {
            StartWatchers();
        }
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

    private void AddFileWatcher(string folder, string path)
    {
        if (!StringUtils.IsMd5(folder))
        {
            return;
        }

        if (folder == StringUtils.EncryptKeyMd5)
        {
            return;
        }

        var fileWatcher = new FileSystemWatcher
        {
            Path = path,
            Filter = "SyncLog.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        fileWatcher.Changed += FileWatcher_OnChanged;
        fileWatcher.EnableRaisingEvents = false;

        _filesWatchers.Add(folder, fileWatcher);
    }

    private void DirectoryWatcher_OnCreated(object sender, FileSystemEventArgs e)
    {
        var fileName = e.Name;
        var filePath = e.FullPath;
        if (!string.IsNullOrEmpty(fileName))
        {
            AddFileWatcher(fileName, filePath);
        }
    }

    private void DirectoryWatcher_OnDeleted(object sender, FileSystemEventArgs e)
    {
        var fileName = e.Name;
        if (!string.IsNullOrEmpty(fileName))
        {
            _filesWatchers.Remove(fileName);
        }
    }

    private void FileWatcher_OnChanged(object sender, FileSystemEventArgs e)
    {
        
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
            _directoryWatcher.Changed -= DirectoryWatcher_OnCreated;
            _directoryWatcher.Deleted -= DirectoryWatcher_OnDeleted;
            _directoryWatcher.Dispose();
            foreach (var watcher in _filesWatchers)
            {
                watcher.Value.Changed -= FileWatcher_OnChanged;
                watcher.Value.Dispose();
            }
            _filesWatchers.Clear();
            _disposed = true;
        }
    }

    #endregion
}

public class SyncDataEventArgs
{

}
