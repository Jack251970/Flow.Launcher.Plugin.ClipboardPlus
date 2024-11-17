using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class SyncStatus : JsonStorage<List<SyncStatusItem>>
{
    private static string ClassName => typeof(SyncStatus).Name;

    private const int DatabaseExportDelay = 300;

    private const int SyncDelay = 3000;

    private readonly IClipboardPlus ClipboardPlus;

    private readonly SyncLog LocalSyncLog;

    private readonly string _localSyncLogPath = PathHelper.SyncLogPath;

    private bool CloudSyncEnabled => ClipboardPlus.Settings.SyncEnabled;

    private string _cloudSyncDiretory;
    private string _cloudSyncLogPath;
    private string _cloudDataPath;

    #region Queued Tasks

    private readonly ConcurrentQueue<Func<Task>> _taskQueue = new();
    private readonly SemaphoreSlim _queueSemaphore = new(1, 1);  // Semaphore to synchronize task queue processing
    private bool _isProcessingQueue;

    #endregion

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
                    await ProcessTaskQueueAsync(async () =>
                    {
                        // write sync log
                        await LocalSyncLog.WriteCloudFileAsync(_cloudSyncLogPath);
                        await Task.Delay(DatabaseExportDelay);  // wait for cloud drive to sync

                        // export database
                        var hashId = _jsonData[index].HashId;
                        var version = _jsonData[index].JsonFileVersion;
                        await DatabaseHelper.ExportDatabase(ClipboardPlus, _cloudDataPath, hashId, version);

                        ClipboardPlus.Context?.API.LogInfo(ClassName, "Sync log & data saved");

                        await Task.Delay(SyncDelay);  // wait for cloud drive to sync
                    });
                }
            }

            return success;
        }

        return false;
    }

    public void DeleteLocalFiles()
    {
        File.Delete(_localSyncLogPath);
        File.Delete(_path);
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

    #region Sync Watcher

    public async Task InitializeSyncData(List<SyncDataEventArgs> args)
    {
        var needReindex = false;

        // check all databases in the sync status file but not in the event args
        var deletedEncryptKeyMd5s = new List<string>();
        foreach (var status in _jsonData)
        {
            // skip the local sync log
            var encryptKeyMd5 = status.EncryptKeyMd5;
            if (encryptKeyMd5 == StringUtils.EncryptKeyMd5)
            {
                continue;
            }

            // find the database in the sync status file but not in the event args
            var index = args.FindIndex(x => x.EncryptKeyMd5 == encryptKeyMd5);
            if (index == -1)
            {
                // need to do operation later for unchanging _jsonData
                deletedEncryptKeyMd5s.Add(encryptKeyMd5);
            }
        }
        foreach (var encryptKeyMd5 in deletedEncryptKeyMd5s)
        {
            // delete database
            await DeleteDatabase(encryptKeyMd5);

            // delete from sync status file
            await DeleteJsonData(encryptKeyMd5);
        }
        needReindex = deletedEncryptKeyMd5s.Count > 0;
        deletedEncryptKeyMd5s.Clear();

        // check all databases in the event args
        foreach (var arg in args)
        {
            // check event args
            if (arg.EventType != SyncEventType.Init)
            {
                continue;
            }

            // read sync data file & parse results
            var folderPath = arg.FolderPath;
            var dataFile = Path.Combine(folderPath, PathHelper.SyncDataFile);
            var results = await DatabaseHelper.ImportDatabase(dataFile);
            if (results == null)
            {
                continue;
            }
            var hashId = results.Value.HashId;
            var version = results.Value.Version;
            var data = results.Value.Data;

            // find the database in the event args but not in sync status file
            var encryptKeyMd5 = arg.EncryptKeyMd5;
            var index = _jsonData.FindIndex(x => x.EncryptKeyMd5 == encryptKeyMd5);
            if (index == -1)
            {
                // add database
                await AddDatabase(data);

                // add into sync status file
                await AddJsonData(hashId, encryptKeyMd5, version);

                needReindex = true;

                continue;
            }

            // check hashId to decide if need to delete records and add database
            var status = _jsonData[index];
            var statusHashId = status.HashId;
            if (hashId != statusHashId)
            {
                // delete database
                await DeleteDatabase(encryptKeyMd5);

                // add database
                await AddDatabase(data);

                // change sync status file
                await ChangeJsonData(encryptKeyMd5, hashId, null);

                needReindex = true;

                continue;
            }

            // check version to decide if need to update records
            var statusVersion = status.JsonFileVersion;
            if (statusVersion >= version)
            {
                return;
            }

            // read sync log file
            var logFile = Path.Combine(folderPath, PathHelper.SyncLogFile);
            var syncLog = new SyncLog(logFile);
            if (!await syncLog.ReadFileAsync())
            {
                return;
            }

            // update database
            var logDatas = syncLog.GetUpdateLogDatas(statusVersion);
            var changed = await UpdateDatabase(logDatas, !needReindex);
            if (!changed)
            {
                needReindex = true;
            }

            // change sync status file
            await ChangeJsonData(encryptKeyMd5, null, version);
        }

        // reindex records in the list
        if (needReindex)
        {
            await ClipboardPlus.InitRecordsFromDatabaseAsync();
        }

        ClipboardPlus.Context?.API.LogInfo(ClassName, "Sync data initialized");
    }

    public async void SyncWatcher_OnSyncDataChanged(object? _, SyncDataEventArgs arg)
    {
        // check event args
        if (arg.EventType == SyncEventType.Init)
        {
            return;
        }

        // if event type is delete, delete database & sync status file
        var encryptKeyMd5 = arg.EncryptKeyMd5;
        if (arg.EventType == SyncEventType.Delete)
        {
            // delete database
            await DeleteDatabase(encryptKeyMd5);

            // delete from sync status file
            await DeleteJsonData(encryptKeyMd5);

            // reindex records in the list
            await ClipboardPlus.InitRecordsFromDatabaseAsync();

            return;
        }

        // read sync data file & parse results
        var folderPath = arg.FolderPath;
        var dataFile = Path.Combine(folderPath, PathHelper.SyncDataFile);
        var results = await DatabaseHelper.ImportDatabase(dataFile);
        if (results == null)
        {
            return;
        }
        var hashId = results.Value.HashId;
        var version = results.Value.Version;
        var data = results.Value.Data;

        // if event type is add, add database & sync status file
        var index = _jsonData.FindIndex(x => x.EncryptKeyMd5 == encryptKeyMd5);
        if (arg.EventType == SyncEventType.Add)
        {
            if (index == -1)
            {
                // add database
                await AddDatabase(data);

                // add into sync status file
                await AddJsonData(hashId, encryptKeyMd5, version);

                // reindex records in the list
                await ClipboardPlus.InitRecordsFromDatabaseAsync();
            }

            return;
        }

        // check hashId to decide if need to delete records and add database
        var status = _jsonData[index];
        var statusHashId = status.HashId;
        if (hashId != statusHashId)
        {
            // delete database
            await DeleteDatabase(encryptKeyMd5);

            // add database
            await AddDatabase(data);

            // change sync status file
            await ChangeJsonData(encryptKeyMd5, hashId, null);

            // reindex records in the list
            await ClipboardPlus.InitRecordsFromDatabaseAsync();

            return;
        }

        // check version to decide if need to update records
        var statusVersion = status.JsonFileVersion;
        if (statusVersion >= version)
        {
            return;
        }

        // read sync log file
        var logFile = Path.Combine(folderPath, PathHelper.SyncLogFile);
        var syncLog = new SyncLog(logFile);
        if (!await syncLog.ReadFileAsync())
        {
            return;
        }

        // update database
        var needReindex = false;
        var logDatas = syncLog.GetUpdateLogDatas(statusVersion);
        var changed = await UpdateDatabase(logDatas, true);
        if (!changed)
        {
            needReindex = true;
        }

        // change sync status file
        await ChangeJsonData(encryptKeyMd5, null, version);

        // reindex records in the list
        if (needReindex)
        {
            await ClipboardPlus.InitRecordsFromDatabaseAsync();
        }

        ClipboardPlus.Context?.API.LogInfo(ClassName, "Sync data changed");
    }

    #endregion

    #region Private

    #region Handle Files

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

            await ProcessTaskQueueAsync(async () =>
            {
                // write sync log
                await LocalSyncLog.InitializeAsync();
                await LocalSyncLog.WriteCloudFileAsync(_cloudSyncLogPath);
                await Task.Delay(DatabaseExportDelay);

                // export database
                await DatabaseHelper.ExportDatabase(ClipboardPlus, _cloudDataPath, hashId, version);

                ClipboardPlus.Context?.API.LogInfo(ClassName, "Sync log & data saved");

                await Task.Delay(SyncDelay);  // wait for cloud drive to sync
            });
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

            await ProcessTaskQueueAsync(async () =>
            {
                // write sync log
                await LocalSyncLog.UpdateFileAsync(version, eventType, datas);
                await LocalSyncLog.WriteCloudFileAsync(_cloudSyncLogPath);
                await Task.Delay(DatabaseExportDelay);

                // export database
                await DatabaseHelper.ExportDatabase(ClipboardPlus, _cloudDataPath, hashId, version);

                ClipboardPlus.Context?.API.LogInfo(ClassName, "Sync log & data saved");

                await Task.Delay(SyncDelay);  // wait for cloud drive to sync
            });
        }
        else
        {
            // write sync log
            await LocalSyncLog.UpdateFileAsync(version, eventType, datas);
        }
    }

    #endregion

    #region Database & Json Data

    private async Task DeleteDatabase(string encryptKeyMd5)
    {
        if (encryptKeyMd5 == StringUtils.EncryptKeyMd5)
        {
            return;
        }

        await ClipboardPlus.Database.DeleteRecordsByEncryptKeyMd5(encryptKeyMd5);
    }

    private async Task DeleteJsonData(string encryptKeyMd5)
    {
        if (encryptKeyMd5 == StringUtils.EncryptKeyMd5)
        {
            return;
        }

        var index = _jsonData.FindIndex(x => x.EncryptKeyMd5 == encryptKeyMd5);
        if (index != -1)
        {
            _jsonData.RemoveAt(index);
            await WriteAsync();
        }
    }

    private async Task AddDatabase(IEnumerable<JsonClipboardData> data)
    {
        var records = data.Select(item => ClipboardData.FromJsonClipboardData(item, true));
        await ClipboardPlus.Database.AddRecordsAsync(records, true, false);
    }

    private async Task AddJsonData(string hashId, string encryptKeyMd5, int version)
    {
        _jsonData.Add(new SyncStatusItem()
        {
            HashId = hashId,
            EncryptKeyMd5 = encryptKeyMd5,
            JsonFileVersion = version
        });
        await WriteAsync();
    }

    private async Task<bool> UpdateDatabase(List<SyncLogItem> logItems, bool changeList)
    {
        var addedClipboardData = new List<ClipboardData>();
        var deletedHashIds = new List<string>();
        var changedClipboardData = new List<ClipboardData>();
        for (var i = 0; i < logItems.Count; i++)  // in chronological order
        {
            var logItem = logItems[i];
            var eventType = logItem.LogEventType;
            var datas = logItem.LogClipboardDatas;
            switch (eventType)
            {
                case EventType.Add:
                    // add all records
                    addedClipboardData.AddRange(datas.Select(item => ClipboardData.FromJsonClipboardData(item, true)));
                    break;
                case EventType.Delete:
                    // find if the record is already in the added list
                    foreach (var data in datas)
                    {
                        var addedIndex = addedClipboardData.FindIndex(x => x.HashId == data.HashId);
                        if (addedIndex != -1)  // if the record is already in the added list, delete it
                        {
                            addedClipboardData.RemoveAt(addedIndex);
                            continue;
                        }
                        // else, add it to the deleted list
                        deletedHashIds.Add(data.HashId);
                    }

                    break;
                case EventType.Change:
                    foreach (var data in datas)
                    {
                        var addedIndex = addedClipboardData.FindIndex(x => x.HashId == data.HashId);
                        if (addedIndex != -1)  // if the record is already in the added list, change it
                        {
                            addedClipboardData[addedIndex] = ClipboardData.FromJsonClipboardData(data, true);
                            continue;
                        }
                        // else, add it to the changed list
                        changedClipboardData.Add(ClipboardData.FromJsonClipboardData(data, true));
                    }
                    break;
            }
        }
        await ClipboardPlus.Database.DeleteRecordsAsync(deletedHashIds);
        await ClipboardPlus.Database.AddRecordsAsync(addedClipboardData, true, false);
        await ClipboardPlus.Database.PinRecordsAsync(changedClipboardData);
        return false;
    }

    private async Task ChangeJsonData(string encryptKeyMd5, string? hashId = null, int? version = null)
    {
        var index = _jsonData.FindIndex(x => x.EncryptKeyMd5 == encryptKeyMd5);
        if (index != -1)
        {
            if (hashId != null)
            {
                _jsonData[index].HashId = hashId;
            }
            if (version != null)
            {
                _jsonData[index].JsonFileVersion = version.Value;
            }
            await WriteAsync();
        }
    }

    #endregion

    #region Extension functions

    private async Task ProcessTaskQueueAsync(Func<Task> func)
    {
        _taskQueue.Enqueue(func);

        if (_isProcessingQueue)
        {
            return;
        }

        _isProcessingQueue = true;
        await _queueSemaphore.WaitAsync(); // Ensure only one task queue process at a time

        try
        {
            while (_taskQueue.TryDequeue(out var f))
            {
                await f();
            }
        }
        finally
        {
            _isProcessingQueue = false;
            _queueSemaphore.Release();
        }
    }

    #endregion

    #endregion
}

public class SyncStatusItem
{
    public string HashId { get; set; } = string.Empty;

    public string EncryptKeyMd5 { get; set; } = string.Empty;

    public int JsonFileVersion { get; set; } = -1;
}
