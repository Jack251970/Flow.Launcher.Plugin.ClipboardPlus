using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipboardPlus.Core.Data.Models;

public class Settings
{
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

    [JsonIgnore]
    public List<Tuple<CbContentType, RecordKeepTime>> KeepTimePairs => 
        new ()
        {
            new(CbContentType.Text, KeepTextHours),
            new(CbContentType.Image, KeepImageHours),
            new(CbContentType.Files, KeepFileHours),
        };

    public void Save()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(PathHelpers.SettingsPath, JsonSerializer.Serialize(this, options));
    }

    public static Settings Load(string filePath)
    {
        var options = new JsonSerializerOptions() { WriteIndented = true };
        using var fs = File.OpenRead(filePath);
        return JsonSerializer.Deserialize<Settings>(fs, options) ?? new Settings();
    }

    public override string ToString()
    {
        var type = GetType();
        var props = type.GetProperties();
        var s = props.Aggregate(
            "Settings(\n",
            (current, prop) =>
            {
                if (prop.Name == nameof(KeepTimePairs))
                {
                    return current;
                }
                return current + $"\t{prop.Name}: {prop.GetValue(this)}\n";
            }
        );
        s += ")";
        return s;
    }
}
