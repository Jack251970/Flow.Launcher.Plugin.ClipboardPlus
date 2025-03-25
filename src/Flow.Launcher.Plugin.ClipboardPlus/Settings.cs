using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Flow.Launcher.Plugin.ClipboardPlus;

public class Settings : ISettings
{
    public string ClearKeyword { get; set; } = "clear";

    public int MaxRecords { get; set; } = 10000;

    public RecordOrder RecordOrder { get; set; } = RecordOrder.CreateTime;

    public bool ActionTop { get; set; } = true;

    public bool ShowNotification { get; set; } = true;

    public bool SyncWindowsClipboardHistory { get; set; } = false;

    public bool UseWindowsClipboardHistoryOnly { get; set; } = false;

    public ClickAction ClickAction { get; set; } = ClickAction.Copy;

    public DefaultRichTextCopyOption DefaultRichTextCopyOption { get; set; } = DefaultRichTextCopyOption.Rtf;

    public DefaultImageCopyOption DefaultImageCopyOption { get; set; } = DefaultImageCopyOption.Image;

    public DefaultFilesCopyOption DefaultFilesCopyOption { get; set; } = DefaultFilesCopyOption.Files;

    public bool CacheImages { get; set; } = false;

    public string CacheFormat { get; set; } = "yyyy-MM-dd-hhmmss-app";

    public bool EncryptData { get; set; } = false;

    public string EncryptKey { get; set; } = StringUtils.GenerateEncryptKey();

    public bool KeepText { get; set; } = false;

    public KeepTime TextKeepTime { get; set; } = 0;

    public bool KeepImages { get; set; } = false;

    public KeepTime ImagesKeepTime { get; set; } = 0;

    public bool KeepFiles { get; set; } = false;

    public KeepTime FilesKeepTime { get; set; } = 0;

    [JsonIgnore]
    public List<Tuple<DataType, KeepTime>> KeepTimePairs => 
        new ()
        {
            new(DataType.PlainText, TextKeepTime),
            new(DataType.RichText, TextKeepTime),
            new(DataType.Image, ImagesKeepTime),
            new(DataType.Files, FilesKeepTime),
        };

    public void RestoreToDefault()
    {
        var defaultSettings = new Settings();
        var type = GetType();
        var props = type.GetProperties();
        foreach (var prop in props)
        {
            if (CheckJsonIgnoredOrKeyAttribute(prop))
            {
                continue;
            }
            var defaultValue = prop.GetValue(defaultSettings);
            prop.SetValue(this, defaultValue);
        }
    }

    public override string ToString()
    {
        var type = GetType();
        var props = type.GetProperties();
        var s = props.Aggregate(
            "Settings(\n",
            (current, prop) =>
            {
                if (CheckJsonIgnoredOrKeyAttribute(prop))
                {
                    return current;
                }
                return current + $"\t{prop.Name}: {prop.GetValue(this)}\n";
            }
        );
        s += ")";
        return s;
    }

    private static bool CheckJsonIgnoredOrKeyAttribute(PropertyInfo prop)
    {
        return
            // JsonIgnored
            prop.GetCustomAttribute<JsonIgnoreAttribute>() != null ||
            // Is EncryptKey
            prop.Name == nameof(EncryptKey);
    }
}
