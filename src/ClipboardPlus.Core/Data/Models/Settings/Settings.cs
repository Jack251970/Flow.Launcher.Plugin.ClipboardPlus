using System.Text.Json;

namespace ClipboardPlus.Core.Data.Models;

public class Settings
{
    public readonly string DbPath = "ClipboardPlus.db";
    public string ConfigFile = string.Empty;

    public string ClearKeyword { get; set; } = "clear";
    public int MaxDataCount { get; set; } = 10000;
    public CbOrders OrderBy { get; set; } = CbOrders.Score;
    public bool CacheImages { get; set; } = false;
    public string ImageFormat { get; set; } = "yyyy-MM-dd-hhmmss-{app}";
    public bool KeepText { get; set; } = false;
    public RecordKeepTime KeepTextHours { get; set; } = 0;
    public bool KeepImage { get; set; } = false;
    public RecordKeepTime KeepImageHours { get; set; } = 0;
    public bool KeepFile { get; set; } = false;
    public RecordKeepTime KeepFileHours { get; set; } = 0;

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigFile, JsonSerializer.Serialize(this, options));
    }

    public static Settings Load(string filePath)
    {
        var options = new JsonSerializerOptions() { WriteIndented = true };
        using var fs = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<Settings>(fs, options)
            ?? new Settings() { ConfigFile = filePath };
    }

    public override string ToString()
    {
        var type = GetType();
        var props = type.GetProperties();
        var s = props.Aggregate(
            "Settings(\n",
            (current, prop) => current + $"\t{prop.Name}: {prop.GetValue(this)}\n"
        );
        s += ")";
        return s;
    }
}
