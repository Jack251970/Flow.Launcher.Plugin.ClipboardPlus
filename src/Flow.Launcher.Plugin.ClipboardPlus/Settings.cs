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

    public ClickAction ClickAction { get; set; } = ClickAction.Copy;

    public DefaultRichTextCopyOption DefaultRichTextCopyOption { get; set; } = DefaultRichTextCopyOption.Rtf;

    public DefaultImageCopyOption DefaultImageCopyOption { get; set; } = DefaultImageCopyOption.Image;

    public DefaultFilesCopyOption DefaultFilesCopyOption { get; set; } = DefaultFilesCopyOption.Files;

    public bool ActionTop { get; set; } = true;

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
            new(DataType.UnicodeText, TextKeepTime),
            new(DataType.RichText, TextKeepTime),
            new(DataType.Image, ImagesKeepTime),
            new(DataType.Files, FilesKeepTime),
        };

    public override string ToString()
    {
        var type = GetType();
        var props = type.GetProperties();
        var s = props.Aggregate(
            "Settings(\n",
            (current, prop) =>
            {
                if (CheckJsonIgnoredAttribute(prop))
                {
                    return current;
                }
                return current + $"\t{prop.Name}: {prop.GetValue(this)}\n";
            }
        );
        s += ")";
        return s;
    }

    private static bool CheckJsonIgnoredAttribute(PropertyInfo prop)
    {
        return prop.GetCustomAttribute<JsonIgnoreAttribute>() != null;
    }
}
