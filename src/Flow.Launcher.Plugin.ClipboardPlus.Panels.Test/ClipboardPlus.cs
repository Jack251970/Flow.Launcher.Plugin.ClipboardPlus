using System.Globalization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

internal class ClipboardPlus : IClipboardPlus, IAsyncDisposable
{
    public PluginInitContext? Context => null;

    public bool UseWindowsClipboardHistoryOnly => false;

    public ObservableDataFormats ObservableDataFormats { get; private set; } = new()
    {
        Images = true,
        Texts = true,
        Files = true,
        Others = false
    };

    public SqliteDatabase Database { get; private set; }

    public ScoreHelper ScoreHelper { get; } = new ScoreHelper(1);

    public ISettings Settings { get; }

    public CultureInfo CultureInfo { get; private set; } = new CultureInfo("en-US");

    public event EventHandler<CultureInfo>? CultureInfoChanged;

    public ClipboardPlus()
    {
        Settings = new Settings();
        StringUtils.InitEncryptKey(Settings.EncryptKey);
        Database = new SqliteDatabase(Path.Combine(AppContext.BaseDirectory, "ClipboardPlus.db"), this);
    }

    public void DisableWindowsClipboardHelper(bool remove)
    {

    }

    public void EnableWindowsClipboardHelper(bool load)
    {

    }

    public ClipboardData GetClipboardDataItem(object? content, DataType dataType, string hashId, DateTime createTime, SourceApplication source, string clipboardText, string clipboardRtfText)
    {
        return ClipboardData.NULL;
    }

    public async Task InitRecordsFromDatabaseAndSystemAsync(bool database, bool system)
    {
        await Task.CompletedTask;
    }

    public ISettings LoadSettingJsonStorage()
    {
        return Settings;
    }

    public void OnCultureInfoChanged(CultureInfo cultureInfo)
    {
        CultureInfo = cultureInfo;
        CultureInfoChanged?.Invoke(this, cultureInfo);
    }

    public void SaveSettingJsonStorage()
    {
        
    }

    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await Database.DisposeAsync();
            Database = null!;
            GarbageCollect();
            _disposed = true;
        }
    }

    private static void GarbageCollect()
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
    }
}
