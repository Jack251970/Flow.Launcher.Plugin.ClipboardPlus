using System.Diagnostics.CodeAnalysis;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class SyncStatus : JsonStorage<List<SyncStatusItem>>
{
    private readonly IClipboardPlus ClipboardPlus;

    private readonly SyncLog LocalSyncLog;

    private readonly string _localSyncLogPath = PathHelper.SyncLogPath;

    private bool CloudSyncEnabled => ClipboardPlus.Settings.SyncEnabled;

    private string _cloudSyncDiretory;
    private string _cloudSyncLogPath;
    private string _cloudDataPath;

    public SyncStatus(IClipboardPlus clipboardPlus, string path) : base(path)
    {
        ClipboardPlus = clipboardPlus;
        LocalSyncLog = new SyncLog(_localSyncLogPath);
        ChangeSyncDatabasePath(clipboardPlus.Settings.SyncDatabasePath);
    }

    public async Task InitializeAsync()
    {
        // write default sync status
        var hashId = StringUtils.GetGuid();
        _jsonData = new()
        {
            new SyncStatusItem()
            {
                HashId = hashId,
                EncryptKeyMd5 = StringUtils.EncryptKeyMd5,
                JsonFileVersion = 0
            }
        };

        // write into files
        await InitializeStatusLogJsonFile(hashId, 0);
    }

    public async Task<bool> ReadFileAsync()
    {
        if (File.Exists(_path) && File.Exists(_localSyncLogPath))
        {
            // read sync status & local sync log files
            var success = await ReadAsync() && await LocalSyncLog.ReadFileAsync();

            // if not success, return
            if (!success)
            {
                return false;
            }

            // if no local sync log data, return
            var index = _jsonData.FindIndex(x => x.EncryptKeyMd5 == StringUtils.EncryptKeyMd5);
            if (index == -1)
            {
                return false;
            }

            if (CloudSyncEnabled)
            {
                // create sync database directory
                if (!Directory.Exists(_cloudSyncDiretory))
                {
                    Directory.CreateDirectory(_cloudSyncDiretory);
                }

                // check if cloud files are exist
                if (!File.Exists(_cloudSyncLogPath))
                {
                    // write sync log
                    await LocalSyncLog.WriteCloudFileAsync(_cloudSyncLogPath);

                    // export database
                    var hashId = _jsonData[index].HashId;
                    var version = _jsonData[index].JsonFileVersion;
                    await DatabaseHelper.ExportDatabase(ClipboardPlus, _cloudDataPath, hashId, version);
                }
            }

            return success;
        }

        return false;
    }

    public async Task UpdateFileAsync(EventType eventType, List<JsonClipboardData> datas)
    {
        if (eventType == EventType.None)
        {
            return;
        }

        var index = _jsonData.FindIndex(x => x.EncryptKeyMd5 == StringUtils.EncryptKeyMd5);
        if (index == -1)
        {
            return;
        }

        switch (eventType)
        {
            case EventType.DeleteAll:
                // generate a new hash id
                var hashId = StringUtils.GetGuid();
                _jsonData[index].HashId = hashId;
                _jsonData[index].JsonFileVersion = 0;

                // write into files
                await InitializeStatusLogJsonFile(hashId, 0);
                break;
            default:
                // if no data, return
                if (datas.Count == 0)
                {
                    return;
                }

                // generate next version
                var nextVersion = _jsonData[index].JsonFileVersion + 1;
                _jsonData[index].JsonFileVersion = nextVersion;

                // write sync log file
                await WriteStatusLogJsonFile(_jsonData[index].HashId, nextVersion, eventType, datas);
                break;
        }
    }

    [MemberNotNull(nameof(_cloudSyncDiretory), nameof(_cloudSyncLogPath), nameof(_cloudDataPath))]
    public void ChangeSyncDatabasePath(string syncDatabasePath)
    {
        _cloudSyncDiretory = Path.Combine(syncDatabasePath, StringUtils.EncryptKeyMd5);
        _cloudSyncLogPath = Path.Combine(_cloudSyncDiretory, PathHelper.SyncLogFile);
        _cloudDataPath = Path.Combine(_cloudSyncDiretory, PathHelper.SyncDataFile);
    }

    public async Task InitializeSyncData(List<SyncDataEventArgs> args)
    {
        // TODO: Handle Delete & InitializeSyncData
        // TODO: Remove test codes
        using var writer = new StreamWriter("D:\\log.txt", true);
        foreach (var e in args)
        {
            writer.WriteLine($"{DateTime.Now}: {e.EventType} Md5: {e.EncryptKeyMd5}");
        }
    }

    private async Task InitializeSyncData(SyncDataEventArgs e)
    {
        // handle event args
        if (e.EventType != SyncEventType.Init)
        {
            return;
        }
        var encryptKeyMd5 = e.EncryptKeyMd5;
        var folderPath = e.FolderPath;

        // read sync data file
        var dataFile = Path.Combine(folderPath, PathHelper.SyncDataFile);
        var results = await DatabaseHelper.ImportDatabase(dataFile);
        if (results == null)
        {
            return;
        }

        var hashId = results.Value.Item1;
        var version = results.Value.Item2;
        var data = results.Value.Item3;

        // if not found encrypt key md5 in sync status
        var index = _jsonData.FindIndex(x => x.EncryptKeyMd5 == encryptKeyMd5);
        if (index == -1)
        {
            return;
        }

        // import database
        var records = data.Select(item => ClipboardData.FromJsonClipboardData(item, true));
        await ClipboardPlus.Database.AddRecordsAsync(records, true, false);

        // write sync status file
        _jsonData.Add(new SyncStatusItem()
        {
            HashId = hashId,
            EncryptKeyMd5 = encryptKeyMd5,
            JsonFileVersion = version
        });
        await WriteAsync();

        // read sync log file
        var logFile = Path.Combine(folderPath, PathHelper.SyncLogFile);
        var syncLog = new SyncLog(logFile);
        if (!await syncLog.ReadFileAsync())
        {
            return;
        }

        // update database
        var curVersion = index == -1 ? version : _jsonData[index].JsonFileVersion;
        var logDatas = syncLog.GetUpdateLogDatas(curVersion);
        // TODO
        /*var records = logDatas.Select(item => ClipboardData.FromJsonClipboardData(item, true));
        await ClipboardPlus.Database.AddRecordsAsync(records, true, false);
        // update sync status file
        if (index != -1)
        {
            _jsonData[index].JsonFileVersion = logVersion;
            await WriteAsync();
        }*/
    }

    public async void SyncWatcher_OnSyncDataChanged(object? _, SyncDataEventArgs e)
    {
        // read sync data file & sync log file
        var dataFile = Path.Combine(e.FolderPath, PathHelper.SyncDataFile);
        var results = await DatabaseHelper.ImportDatabase(dataFile);
        if (results == null)
        {
            return;
        }

        var logFile = Path.Combine(e.FolderPath, PathHelper.SyncLogFile);
        var syncLog = new SyncLog(logFile);
        if (!await syncLog.ReadFileAsync())
        {
            return;
        }

        var hashId = results.Value.Item1;
        var version = results.Value.Item2;
        var data = results.Value.Item3;

        // handle event type
        switch (e.EventType)
        {
            case SyncEventType.Add:
                break;
            case SyncEventType.Delete:
                break;
            case SyncEventType.Change:
                break;
        }
        
        // TODO: Remove test codes
        using var writer = new StreamWriter("D:\\log.txt", true);
        writer.WriteLine($"{DateTime.Now}: {e.EventType} Md5: {e.EncryptKeyMd5} Hash: {hashId} Ver: {version}");
    }

    private async Task InitializeStatusLogJsonFile(string hashId, int version)
    {
        // write sync status file
        await WriteAsync();

        if (CloudSyncEnabled)
        {
            // create sync database directory
            if (!Directory.Exists(_cloudSyncDiretory))
            {
                Directory.CreateDirectory(_cloudSyncDiretory);
            }

            // write sync log
            await LocalSyncLog.InitializeAsync();
            await LocalSyncLog.WriteCloudFileAsync(_cloudSyncLogPath);

            // export database
            await DatabaseHelper.ExportDatabase(ClipboardPlus, _cloudDataPath, hashId, version);
        }
        else
        {
            // write sync log
            await LocalSyncLog.InitializeAsync();
        }
    }

    private async Task WriteStatusLogJsonFile(string hashId, int version, EventType eventType, List<JsonClipboardData> datas)
    {
        // write sync status file
        await WriteAsync();

        if (CloudSyncEnabled)
        {
            // create sync database directory
            if (!Directory.Exists(_cloudSyncDiretory))
            {
                Directory.CreateDirectory(_cloudSyncDiretory);
            }

            // write sync log
            await LocalSyncLog.UpdateFileAsync(version, eventType, datas);
            await LocalSyncLog.WriteCloudFileAsync(_cloudSyncLogPath);

            // export database
            await DatabaseHelper.ExportDatabase(ClipboardPlus, _cloudDataPath, hashId, version);
        }
        else
        {
            // write sync log
            await LocalSyncLog.UpdateFileAsync(version, eventType, datas);
        }
    }
}

public class SyncStatusItem
{
    public string HashId { get; set; } = string.Empty;

    public string EncryptKeyMd5 { get; set; } = string.Empty;

    public int JsonFileVersion { get; set; } = -1;
}
