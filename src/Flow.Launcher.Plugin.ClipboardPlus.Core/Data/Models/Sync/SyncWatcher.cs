namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class SyncWatcher : IDisposable
{
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
        _directoryWatcher.EnableRaisingEvents = true;

        var folders = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
        foreach (var folder in folders)
        {
            var folderName = Path.GetFileName(folder);
            AddFileWatcher(folderName, folder);
        }
    }

    private void AddFileWatcher(string folder, string path)
    {
        if (!StringUtils.IsMd5(folder))
        {
            return;
        }

        Console.WriteLine($"File watcher added: {folder} {path}");
        var fileWatcher = new FileSystemWatcher
        {
            Path = path,
            Filter = "SyncLog.json",
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName
        };
        fileWatcher.Changed += FileWatcher_OnChanged;
        fileWatcher.EnableRaisingEvents = true;

        _filesWatchers.Add(folder, fileWatcher);
    }

    private void DirectoryWatcher_OnCreated(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Folder created: {e.Name} {e.FullPath}");
        var fileName = e.Name;
        var filePath = e.FullPath;
        if (!string.IsNullOrEmpty(fileName))
        {
            AddFileWatcher(fileName, filePath);
        }
    }

    private void DirectoryWatcher_OnDeleted(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"Folder deleted: {e.FullPath}");
        var fileName = e.Name;
        if (!string.IsNullOrEmpty(fileName))
        {
            _filesWatchers.Remove(fileName);
        }
    }

    private void FileWatcher_OnChanged(object sender, FileSystemEventArgs e)
    {
        Console.WriteLine($"File changed: {e.FullPath}");
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
            _disposed = true;
        }
    }

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }

    #endregion
}
