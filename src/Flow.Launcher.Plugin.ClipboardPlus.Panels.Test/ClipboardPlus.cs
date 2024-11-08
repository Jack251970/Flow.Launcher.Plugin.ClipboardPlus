using System.Globalization;
using System;
using System.IO;
using System.Threading.Tasks;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

internal class ClipboardPlus : IClipboardPlus
{
    public PluginInitContext? Context => null;

    public SqliteDatabase Database { get; } = new SqliteDatabase(Path.Combine(AppContext.BaseDirectory, "ClipboardPlus.db"), 1);

    public async Task InitRecordsFromDatabaseAsync()
    {
        await Task.CompletedTask;
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

    public event EventHandler<CultureInfo>? CultureInfoChanged;

    public void OnCultureInfoChanged(CultureInfo cultureInfo)
    {
        CultureInfo = cultureInfo;
        CultureInfoChanged?.Invoke(this, cultureInfo);
    }
}
