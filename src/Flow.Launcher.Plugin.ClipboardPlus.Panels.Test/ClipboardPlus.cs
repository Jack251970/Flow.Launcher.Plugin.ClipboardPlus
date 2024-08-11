using System.Globalization;
using System;

namespace Flow.Launcher.Plugin.ClipboardPlus.Panels.Test;

internal class ClipboardPlus : IClipboardPlus
{
    public PluginInitContext? Context => null;

    public ISettings Settings { get; }

    public ClipboardPlus()
    {
        Settings = new Settings();
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
