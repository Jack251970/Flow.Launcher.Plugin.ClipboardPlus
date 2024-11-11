namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public static class SyncHelper
{
    private static bool syncInitialized = false;
    public static bool SyncInitialized => syncInitialized;

    private static SyncStatus? syncStatus;
    public static SyncStatus? SyncStatus => syncStatus;

    public static async Task InitializeAsync(IClipboardPlus clipboardPlus)
    {
        // if already initialized
        if (syncInitialized)
        {
            return;
        }

        await ReinitializeAsync(clipboardPlus);
    }

    public static async Task ReinitializeAsync(IClipboardPlus clipboardPlus)
    {
        // if sync status file exists
        if (File.Exists(PathHelper.SyncStatusPath))
        {
            // read sync status
            syncStatus = new SyncStatus(clipboardPlus, PathHelper.SyncStatusPath);
            if (!await syncStatus.ReadFileAsync())
            {
                // reinitialize files
                await syncStatus!.InitializeAsync();
            }

            // set sync initialized
            syncInitialized = true;
            return;
        }

        // check if need to initialize sync status
        if (clipboardPlus != null)
        {
            // if need to sync database and sync database path is valid
            var settings = clipboardPlus.Settings;
            if (settings.SyncDatabase && (!string.IsNullOrEmpty(settings.SyncDatabasePath)))
            {
                // create sync database directory
                if (!Directory.Exists(settings.SyncDatabasePath))
                {
                    Directory.CreateDirectory(settings.SyncDatabasePath);
                }

                // initialize files
                syncStatus = new SyncStatus(clipboardPlus, PathHelper.SyncStatusPath);
                await syncStatus!.InitializeAsync();

                // set sync initialized
                syncInitialized = true;
            }
        }
    }
}
