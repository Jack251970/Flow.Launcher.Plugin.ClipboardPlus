using System.Text.Json;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public static class DatabaseHelper
{
    public static async Task ExportDatabase(IClipboardPlus clipboardPlus, string jsonPath)
    {
        var database = clipboardPlus.Database;
        var records = await database.GetAllRecordsAsync(false);
        var jsonRecords = records.Select(JsonClipboardData.FromClipboardData);
        var addedCount = jsonRecords.Count();
        var options = new JsonSerializerOptions { WriteIndented = true };
        await using FileStream createStream = File.Create(jsonPath);
        await JsonSerializer.SerializeAsync(createStream, jsonRecords, options);
        var context = clipboardPlus.Context;
        context?.API.ShowMsg(context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
            string.Format(context.GetTranslation("flowlauncher_plugin_clipboardplus_export_succeeded"), addedCount));
    }

    public static async Task ImportDatabase(IClipboardPlus clipboardPlus, string jsonPath)
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
        var context = clipboardPlus.Context;
        if (jsonRecords != null)
        {
            var database = clipboardPlus.Database;
            var records = jsonRecords.Select(r => ClipboardData.FromJsonClipboardData(r, true));
            var databaseRecords = await database.GetAllRecordsAsync(false);
            var addedCount = 0;
            if (!databaseRecords.Any())  // if there are no records in the database, then add all records
            {
                await database.AddRecordsAsync(records, true);
                addedCount = records.Count();
            }
            else  // if there are records in the database, then add only the records that are not already in the database
            {
                var addedClipboardData = new List<ClipboardData>();
                foreach (var record in records)
                {
                    // if hashId & encryptKeyMd5 are equal, then the record is already in the database
                    if (databaseRecords.Any(r => r.HashId == record.HashId && r.EncryptKeyMd5 == record.EncryptKeyMd5))
                    {
                        continue;
                    }
                    addedClipboardData.Add(record);
                }
                await database.AddRecordsAsync(addedClipboardData, true);
                addedCount = addedClipboardData.Count;
            }
            if (addedCount > 0)
            {
                await clipboardPlus.InitRecordsFromDatabaseAsync();
            }
            context?.API.ShowMsg(context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                string.Format(context.GetTranslation("flowlauncher_plugin_clipboardplus_import_succeeded"), addedCount));
        }
        else
        {
            context?.API.ShowMsgError(context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                context.GetTranslation("flowlauncher_plugin_clipboardplus_import_failed"));
        }
    }
}
