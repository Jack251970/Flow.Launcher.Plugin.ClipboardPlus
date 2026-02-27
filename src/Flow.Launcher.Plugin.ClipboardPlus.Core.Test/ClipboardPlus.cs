using Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Contracts;
using System.Globalization;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Test;

internal class ClipboardPlus : IClipboardPlus
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

    public ISettings Settings { get; } = null!;

    public CultureInfo CultureInfo { get; private set; } = new CultureInfo("en-US");

    public event EventHandler<CultureInfo>? CultureInfoChanged;

    public ClipboardPlus()
    {
        Database = new SqliteDatabase(Path.Combine(AppContext.BaseDirectory, "ClipboardPlus.db"), this);
    }

    public void EnableWindowsClipboardHelper(bool load)
    {

    }

    public void DisableWindowsClipboardHelper(bool remove)
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

    public void SaveSettingJsonStorage()
    {

    }

    public void AddExcludedPath(string appPath)
    {
        
    }

    public void RemoveExcludedPath(string appPath)
    {
        
    }

    public void ClearExcludedPath()
    {

    }
}
