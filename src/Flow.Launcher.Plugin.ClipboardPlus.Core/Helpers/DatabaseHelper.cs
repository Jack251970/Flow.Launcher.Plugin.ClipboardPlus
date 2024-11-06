namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public static class DatabaseHelper
{
    public static async Task ExportDatabase(SqliteDatabase database, string path)
    {
        var records = await database.GetAllRecordsAsync();
        var exportDatabase = new SqliteDatabase(path);
        await exportDatabase.InitializeDatabaseAsync();
        await exportDatabase.AddRecordsAsync(records, false);
    }
}
