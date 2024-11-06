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
}
