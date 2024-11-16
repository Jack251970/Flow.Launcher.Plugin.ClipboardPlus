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
                clipboardPlus.Context?.API.LogWarn(ClassName, "Sync status reinitialized");
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
                await InitializeSyncWatcher(clipboardPlus);
            }

            // set sync initialized
            syncStatusInitialized = true;
            clipboardPlus.Context?.API.LogInfo(ClassName, "Sync helper initialized");
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
                await InitializeSyncWatcher(clipboardPlus);

                // set sync initialized
                syncStatusInitialized = true;
                clipboardPlus.Context?.API.LogInfo(ClassName, "Sync helper initialized");
            }
        }
    }

    public static void Disable()
    {
        if (syncStatusInitialized)
        {
            // disable sync watcher
            if (syncWatcherInitialized)
            {
                syncWatcher!.SyncDataInitialized -= syncStatus!.InitializeSyncData;
                syncWatcher!.SyncDataChanged -= syncStatus!.SyncWatcher_OnSyncDataChanged;
                syncWatcher.Dispose();
                syncWatcher = null;
                syncWatcherInitialized = false;
            }

            // disable sync status
            syncStatus = null;
            syncStatusInitialized = false;
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

    public static async void ChangeSyncDatabasePath(IClipboardPlus clipboardPlus)
    {
        if (syncStatusInitialized)
        {
            // change sync status database path
            var syncDatabasePath = clipboardPlus.Settings.SyncDatabasePath;
            syncStatus!.ChangeSyncDatabasePath(syncDatabasePath);

            // change sync watcher database path
            if (syncWatcherInitialized)
            {
                await syncWatcher!.ChangeSyncDatabasePath(syncDatabasePath);
            }
        }
    }

    public static async void ChangeSyncEnabled(IClipboardPlus clipboardPlus)
    {
        if (syncStatusInitialized)
        {
            // if sync watcher is not initialized and need to enable it
            var syncEnabled = clipboardPlus.Settings.SyncEnabled;
            if (syncEnabled)
            {
                await InitializeSyncWatcher(clipboardPlus);
            }

            // change sync enabled
            if (syncWatcherInitialized)
            {
                syncWatcher!.Enabled = syncEnabled;
            }
        }
    }

    private static async Task InitializeSyncWatcher(IClipboardPlus clipboardPlus)
    {
        if (!syncWatcherInitialized)
        {
            syncWatcher = new SyncWatcher();
            syncWatcher.SyncDataInitialized += syncStatus!.InitializeSyncData;
            syncWatcher.SyncDataChanged += syncStatus!.SyncWatcher_OnSyncDataChanged;
            await syncWatcher.InitializeWatchers(clipboardPlus.Settings.SyncDatabasePath);
            syncWatcher.Enabled = true;
            syncWatcherInitialized = true;
            clipboardPlus.Context?.API.LogInfo(ClassName, "Start sync watcher");
        }
    }

    public static void Dispose()
    {
        Disable();
    }
}
