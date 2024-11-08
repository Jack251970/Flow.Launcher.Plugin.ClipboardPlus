using System.Globalization;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Contracts;

public interface IClipboardPlus
{
    public PluginInitContext? Context { get; }

    public SqliteDatabase Database { get; }

    Task InitRecordsFromDatabaseAsync();

    public ISettings Settings { get; }

    public ISettings LoadSettingJsonStorage();

    public void SaveSettingJsonStorage();

    public CultureInfo CultureInfo { get; }

    public event EventHandler<CultureInfo>? CultureInfoChanged;
}
