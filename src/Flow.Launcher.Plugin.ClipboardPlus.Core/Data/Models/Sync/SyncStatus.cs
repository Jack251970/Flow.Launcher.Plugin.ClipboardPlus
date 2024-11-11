namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class SyncStatus : JsonStorage<List<SyncStatusItem>>
{
    private readonly IClipboardPlus ClipboardPlus;

    private readonly SyncLog LocalSyncLog;

    private readonly string _localSyncLogPath = PathHelper.SyncLogPath;

    public SyncStatus(IClipboardPlus clipboardPlus, string path) : base(path)
    {
        ClipboardPlus = clipboardPlus;
        LocalSyncLog = new SyncLog(_localSyncLogPath);
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
            return await ReadAsync() && await LocalSyncLog.ReadFileAsync();
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
        if (index != -1)
        {
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
    }

    private async Task InitializeStatusLogJsonFile(string hashId, int version)
    {
        // write sync status file
        await WriteAsync();

        // write sync log
        await LocalSyncLog.InitializeAsync();

        // export database
        await DatabaseHelper.ExportLocalDatabase(ClipboardPlus, hashId, version);
    }

    private async Task WriteStatusLogJsonFile(string hashId, int version, EventType eventType, List<JsonClipboardData> datas)
    {
        // write sync status file
        await WriteAsync();

        // write sync log
        await LocalSyncLog.UpdateFileAsync(version, eventType, datas);

        // export database
        await DatabaseHelper.ExportLocalDatabase(ClipboardPlus, hashId, version);
    }
}

public class SyncStatusItem
{
    public string HashId { get; set; } = string.Empty;

    public string EncryptKeyMd5 { get; set; } = string.Empty;

    public int JsonFileVersion { get; set; } = -1;
}
