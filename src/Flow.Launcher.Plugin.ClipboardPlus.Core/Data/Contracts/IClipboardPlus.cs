using System.Globalization;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Contracts;

public interface IClipboardPlus
{
    public PluginInitContext? Context { get; }

    public bool UseWindowsClipboardHistoryOnly { get; }

    public IClipboardMonitor ClipboardMonitor { get; }

    public SqliteDatabase Database { get; }

    Task InitRecordsFromDatabaseAndSystemAsync();

    void EnableWindowsClipboardHelper(bool load);

    void DisableWindowsClipboardHelper(bool remove);

    public ISettings Settings { get; }

    public ISettings LoadSettingJsonStorage();

    public void SaveSettingJsonStorage();

    public CultureInfo CultureInfo { get; }

    public event EventHandler<CultureInfo>? CultureInfoChanged;

    public ClipboardData GetClipboardDataItem(object? content, DataType dataType, string hashId, DateTime createTime, SourceApplication source, string clipboardText, string clipboardRtfText);
}
