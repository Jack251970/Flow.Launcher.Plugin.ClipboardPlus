namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class SyncLog : JsonStorage<List<SyncLogItem>>
{
    public SyncLog(string path) : base(path)
    {

    }

    public async Task InitializeAsync()
    {
        // write default sync log
        _jsonData = new()
        {
            new SyncLogItem()
            {
                JsonFileVersion = 0,
                LogEventType = EventType.None,
                LogClipboardDatas = new()
            }
        };
        await WriteAsync();
    }

    public async Task<bool> ReadFileAsync()
    {
        if (File.Exists(_path))
        {
            return await ReadAsync();
        }

        return false;
    }

    public async Task UpdateFileAsync(int version, EventType eventType, List<JsonClipboardData> datas)
    {
        _jsonData.Add(new SyncLogItem()
        {
            JsonFileVersion = version,
            LogEventType = eventType,
            LogClipboardDatas = datas
        });
        await WriteAsync();
    }

    public async Task WriteCloudFileAsync(string cloudFilePath)
    {
        await WriteAsync(cloudFilePath);
    }

    public List<SyncLogItem> GetUpdateLogDatas(int curVersion)
    {
        var latestVersion = GetLatestVersion();
        if (latestVersion < curVersion)
        {
            return new();
        }

        var startIndex = _jsonData.FindIndex(x => x.JsonFileVersion == curVersion);
        var endIndex = _jsonData.Capacity - 1;
        return _jsonData.GetRange(startIndex + 1, endIndex - startIndex);
    }

    private int GetLatestVersion()
    {
        return _jsonData.Last().JsonFileVersion;
    }
}

public class SyncLogItem
{
    public int JsonFileVersion { get; set; } = -1;

    public EventType LogEventType { get; set; } = EventType.None;

    public List<JsonClipboardData> LogClipboardDatas = new();
}

public enum EventType
{
    None,
    Add,
    Change,
    Delete,
    DeleteAll
}
