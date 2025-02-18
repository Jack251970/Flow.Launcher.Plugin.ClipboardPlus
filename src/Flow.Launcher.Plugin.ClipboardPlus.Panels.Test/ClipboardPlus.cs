using System.Globalization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

internal class ClipboardPlus : IClipboardPlus
{
    public PluginInitContext? Context => null;

    public SqliteDatabase Database { get; } = new SqliteDatabase(Path.Combine(AppContext.BaseDirectory, "ClipboardPlus.db"), 1);

    public async Task InitRecordsFromDatabaseAndSystemAsync()
    {
        await Task.CompletedTask;
    }

    public async Task InitRecordsFromSystemAsync()
    {
        await Task.CompletedTask;
    }

    public void EnableWindowsClipboardHelper(bool load)
    {
        
    }

    public void DisableWindowsClipboardHelper(bool remove)
    {
        
    }

    public ISettings Settings { get; }

    public ClipboardPlus()
    {
        Settings = new Settings();
        StringUtils.InitEncryptKey(Settings.EncryptKey);
    }

    public ISettings LoadSettingJsonStorage()
    {
        return Settings;
    }

    public void SaveSettingJsonStorage()
    {
        
    }

    public CultureInfo CultureInfo { get; private set; } = new CultureInfo("en-US");

    public IClipboardMonitor ClipboardMonitor => null!;

    public event EventHandler<CultureInfo>? CultureInfoChanged;

    public void OnCultureInfoChanged(CultureInfo cultureInfo)
    {
        CultureInfo = cultureInfo;
        CultureInfoChanged?.Invoke(this, cultureInfo);
    }

    public ClipboardData GetClipboardDataItem(object? content, DataType dataType, string hashId, DateTime createTime, SourceApplication source, string clipboardText, string clipboardRtfText)
    {
        return ClipboardData.NULL;
    }
}
