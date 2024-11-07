using System.Text.Json;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public static class DatabaseHelper
{
    public static async Task ExportDatabase(IClipboardPlus clipboardPlus, string jsonPath)
    {
        var database = clipboardPlus.Database;
        var records = await database.GetAllRecordsAsync();
        var jsonRecords = records.Select(JsonClipboardData.FromClipboardData);
        var options = new JsonSerializerOptions { WriteIndented = true };
        await using FileStream createStream = File.Create(jsonPath);
        await JsonSerializer.SerializeAsync(createStream, jsonRecords, options);
        var context = clipboardPlus.Context;
        context?.API.ShowMsg(context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
            context.GetTranslation("flowlauncher_plugin_clipboardplus_export_succeeded"));
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
            var databaseRecords = await database.GetAllRecordsAsync();
            if (!databaseRecords.Any())
            {
                // TODO: Add records
            }
            else
            {
                // TODO: Merge records
            }
            context?.API.ShowMsg(context.GetTranslation("flowlauncher_plugin_clipboardplus_success"),
                context.GetTranslation("flowlauncher_plugin_clipboardplus_import_succeeded"));
        }
        else
        {
            context?.API.ShowMsgError(context.GetTranslation("flowlauncher_plugin_clipboardplus_fail"),
                context.GetTranslation("flowlauncher_plugin_clipboardplus_import_failed"));
        }
    }
}
