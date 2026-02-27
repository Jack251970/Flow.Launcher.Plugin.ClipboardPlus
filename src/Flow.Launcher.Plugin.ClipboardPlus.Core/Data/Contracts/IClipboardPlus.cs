using System;
using System.Globalization;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Contracts;

public interface IClipboardPlus
{
    public PluginInitContext? Context { get; }

    public bool UseWindowsClipboardHistoryOnly { get; }

    public ObservableDataFormats ObservableDataFormats { get; }

    public SqliteDatabase Database { get; }

    public ScoreHelper ScoreHelper { get; }

    public Task InitRecordsFromDatabaseAndSystemAsync(bool database, bool system);

    public void EnableWindowsClipboardHelper(bool load);

    public void DisableWindowsClipboardHelper(bool remove);

    public ISettings Settings { get; }

    public ISettings LoadSettingJsonStorage();

    public void SaveSettingJsonStorage();

    public CultureInfo CultureInfo { get; }

    public event EventHandler<CultureInfo>? CultureInfoChanged;

    public ClipboardData GetClipboardDataItem(object? content, DataType dataType, string hashId, DateTime createTime, SourceApplication source, string clipboardText, string clipboardRtfText);

    public void AddExcludedPath(string path);

    public void RemoveExcludedPath(string path);
}
