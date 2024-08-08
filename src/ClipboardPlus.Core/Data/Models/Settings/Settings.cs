using System.Text.Json;
using System.Text.Json.Serialization;

namespace ClipboardPlus.Core.Data.Models;

public class Settings
{
    public string ClearKeyword { get; set; } = "clear";
    public int MaxRecords { get; set; } = 10000;
    public RecordOrder RecordOrder { get; set; } = RecordOrder.Score;
    public ClickAction ClickAction { get; set; } = ClickAction.Copy;
    public bool CacheImages { get; set; } = false;
    public string CacheFormat { get; set; } = "yyyy-MM-dd-hhmmss-app";
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
            new(DataType.Text, TextKeepTime),
            new(DataType.Image, ImagesKeepTime),
            new(DataType.Files, FilesKeepTime),
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
