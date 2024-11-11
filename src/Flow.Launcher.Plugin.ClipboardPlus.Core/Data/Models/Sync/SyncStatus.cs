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
        await WriteAsync();

        // write sync log
        await LocalSyncLog.InitializeAsync();

        // export database
        await DatabaseHelper.ExportLocalDatabase(ClipboardPlus, hashId, 0);
    }

    public async Task<bool> ReadFileAsync()
    {
        if (File.Exists(_path) && File.Exists(_localSyncLogPath))
        {
            return await ReadAsync();
        }

        return false;
    }

    public int GetLocalJsonFileVersion()
    {
        foreach (var item in _jsonData)
        {
            if (item.EncryptKeyMd5 == StringUtils.EncryptKeyMd5)
            {
                return item.JsonFileVersion;
            }
        }

        return -1;
    }

    public async Task<int> UpdateLocalJsonFileVersion()
    {
        foreach (var item in _jsonData)
        {
            if (item.EncryptKeyMd5 == StringUtils.EncryptKeyMd5)
            {
                item.JsonFileVersion++;
                await WriteAsync();
                return item.JsonFileVersion;
            }
        }

        return -1;
    }
}

public class SyncStatusItem
{
    public string HashId { get; set; } = string.Empty;

    public string EncryptKeyMd5 { get; set; } = string.Empty;

    public int JsonFileVersion { get; set; } = -1;
}
