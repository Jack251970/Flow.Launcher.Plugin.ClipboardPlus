namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public static class SyncHelper
{
    private static string ClassName => typeof(SyncHelper).Name;

    private static bool syncStatusInitialized = false;

    private static bool syncWatcherInitialized = false;

    private static SyncStatus? syncStatus;

    private static SyncWatcher? syncWatcher;

    public static async Task InitializeAsync(IClipboardPlus clipboardPlus)
    {
        // if already initialized, return
        if (syncStatusInitialized)
        {
            return;
        }

        // if sync status file exists
        var syncDatabasePath = clipboardPlus.Settings.SyncDatabasePath;
        var syncEnabled = clipboardPlus.Settings.SyncEnabled;
        if (File.Exists(PathHelper.SyncStatusPath))
        {
            // read sync status
            syncStatus = new SyncStatus(clipboardPlus, PathHelper.SyncStatusPath);
            if (!await syncStatus.ReadFileAsync())
            {
                // reinitialize files
                await syncStatus!.InitializeAsync();
            }

            // if sync database enabled and sync database path is valid
            if (syncEnabled)
            {
                // create sync database directory
                if (!Directory.Exists(syncDatabasePath))
                {
                    Directory.CreateDirectory(syncDatabasePath);
                }

                // initialize sync watcher
                InitializeSyncWatcher(clipboardPlus);
            }

            // set sync initialized
            syncStatusInitialized = true;
            clipboardPlus.Context?.API.LogInfo(ClassName, "Sync status initialized");
            return;
        }

        // check if need to initialize sync status
        if (clipboardPlus != null)
        {
            // if sync database enabled and sync database path is valid
            if (syncEnabled)
            {
                // create sync database directory
                if (!Directory.Exists(syncDatabasePath))
                {
                    Directory.CreateDirectory(syncDatabasePath);
                }

                // initialize files
                syncStatus = new SyncStatus(clipboardPlus, PathHelper.SyncStatusPath);
                await syncStatus!.InitializeAsync();

                // initialize sync watcher
                InitializeSyncWatcher(clipboardPlus);

                // set sync initialized
                syncStatusInitialized = true;
                clipboardPlus.Context?.API.LogInfo(ClassName, "Sync status initialized");
            }
        }
    }

    public static void Disable()
    {
        if (syncStatusInitialized)
        {
            // disable sync status
            syncStatus = null;
            syncStatusInitialized = false;

            // disable sync watcher
            if (syncWatcherInitialized)
            {
                syncWatcher!.Dispose();
                syncWatcher = null;
                syncWatcherInitialized = false;
            }
        }
    }

    public static async Task UpdateSyncStatusAsync(EventType eventType, List<JsonClipboardData> datas)
    {
        if (syncStatusInitialized)
        {
            await syncStatus!.UpdateFileAsync(eventType, datas);
        }
    }

    public static async Task UpdateSyncStatusAsync(EventType eventType, JsonClipboardData data)
    {
        if (syncStatusInitialized)
        {
            await syncStatus!.UpdateFileAsync(eventType, new List<JsonClipboardData>() { data });
        }
    }

    public static void ChangeSyncDatabasePath(IClipboardPlus clipboardPlus)
    {
        if (syncStatusInitialized)
        {
            // change sync status database path
            var syncDatabasePath = clipboardPlus.Settings.SyncDatabasePath;
            syncStatus!.ChangeSyncDatabasePath(syncDatabasePath);

            // change sync watcher database path
            if (syncWatcherInitialized)
            {
                syncWatcher!.ChangeSyncDatabasePath(syncDatabasePath);
            }
        }
    }

    public static void ChangeSyncEnabled(IClipboardPlus clipboardPlus)
    {
        if (syncStatusInitialized)
        {
            // if sync watcher is not initialized and need to enable it
            var syncEnabled = clipboardPlus.Settings.SyncEnabled;
            if (syncEnabled)
            {
                InitializeSyncWatcher(clipboardPlus);
            }

            // change sync enabled
            if (syncWatcherInitialized)
            {
                syncWatcher!.Enabled = syncEnabled;
            }
        }
    }

    private static void InitializeSyncWatcher(IClipboardPlus clipboardPlus)
    {
        if (syncWatcherInitialized)
        {
            return;
        }

        syncWatcher = new SyncWatcher();
        syncWatcher.SyncDataChanged += syncStatus!.SyncWatcher_OnSyncDataChanged;
        syncWatcher.InitializeWatchers(clipboardPlus.Settings.SyncDatabasePath);
        syncWatcher.Enabled = true;
        syncWatcherInitialized = true;
        clipboardPlus.Context?.API.LogInfo(ClassName, "Start sync watcher");
    }

    public static void Dispose()
    {
        Disable();
    }
}
