using System.Text.Json;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public static class DatabaseHelper
{
    public static async Task ExportDatabase(SqliteDatabase database, string jsonPath)
    {
        var records = await database.GetAllRecordsAsync();
        var jsonRecords = records.Select(JsonClipboardData.FromClipboardData);
        var options = new JsonSerializerOptions { WriteIndented = true };
        await using FileStream createStream = File.Create(jsonPath);
        await JsonSerializer.SerializeAsync(createStream, jsonRecords, options);
    }

    public static async Task ImportDatabase(SqliteDatabase database, string jsonPath)
    {
        await using FileStream openStream = File.OpenRead(jsonPath);
        List<JsonClipboardData>? jsonRecords = null;
        try
        {
            jsonRecords = await JsonSerializer.DeserializeAsync<List<JsonClipboardData>>(openStream);
        }
        catch (Exception)
        {
            // ignored
        }
        if (jsonRecords != null)
        {
            var records = jsonRecords.Select(r => ClipboardData.FromJsonClipboardData(r, true));
            var databaseRecords = await database.GetAllRecordsAsync();
            if (!databaseRecords.Any())
            {
                // TODO: Add records
            }
            else
            {
                // TODO: Merge records
            }
        }
    }
}
