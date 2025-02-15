namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public static class PathHelper
{
    #region Constants

    public const string TempCacheImageName = "temp";

    private const string SettingsFile = "settings.json";
    private const string DatabaseFile = "ClipboardPlus.db";

    #endregion

    #region Initialization

    private static bool IsInitialized = false;

    public static void Init(PluginInitContext context, string assemblyName)
    {
        if (!IsInitialized)
        {
            // plugin paths
            PluginPath = context.CurrentPluginMetadata.PluginDirectory;
            PluginSettingsPath = GetDataDirectory(assemblyName);
            var originalImageCachePath = Path.Combine(PluginPath, "CachedImages");
            ImageCachePath = Path.Combine(PluginSettingsPath, "CachedImages");
            FileUtils.MoveDirectory(originalImageCachePath, ImageCachePath);
            if (!Directory.Exists(ImageCachePath))
            {
                Directory.CreateDirectory(ImageCachePath);
            }
            FileUtils.ClearImageCache(ImageCachePath, TempCacheImageName);

            // data paths
            var originalDatabasePath = Path.Combine(PluginPath, DatabaseFile);
            DatabasePath = Path.Combine(PluginSettingsPath, DatabaseFile);
            FileUtils.MoveFile(originalDatabasePath, DatabasePath);

            // icons paths
            IconPath = Path.Combine(PluginPath, "Images");
            AppIconPath = Path.Combine(IconPath, "clipboard.png");
            ConnectIconPath = Path.Combine(IconPath, "connect.png");
            DisconnectIconPath = Path.Combine(IconPath, "disconnect.png");
            CleanIconPath = Path.Combine(IconPath, "clean.png");
            ClearIconPath = Path.Combine(IconPath, "clear.png");
            ListIconPath = Path.Combine(IconPath, "list.png");
            DatabaseIconPath = Path.Combine(IconPath, "database.png");
            UnpinIcon1Path = Path.Combine(IconPath, "unpin1.png");
            ErrorIconPath = Path.Combine(IconPath, "error.png");
            TextIconPath = Path.Combine(IconPath, "text.png");
            ImageIconPath = Path.Combine(IconPath, "image.png");
            FileIconPath = Path.Combine(IconPath, "file.png");
            CopyIconPath = Path.Combine(IconPath, "copy.png");
            PinIconPath = Path.Combine(IconPath, "pin.png");
            UnpinIconPath = Path.Combine(IconPath, "unpin.png");
            DeleteIconPath = Path.Combine(IconPath, "delete.png");
            IsInitialized = true;
        }
    }

    #endregion

    #region Properties

    // plugin paths
    public static string PluginPath { get; private set; } = string.Empty;
    public static string PluginSettingsPath { get; private set; } = string.Empty;
    public static string ImageCachePath { get; private set; } = string.Empty;
    public static string IconPath { get; private set; } = string.Empty;

    // data paths
    public static string SettingsPath { get; private set; } = string.Empty;
    public static string DatabasePath { get; private set; } = string.Empty;

    // icons paths
    public static string AppIconPath { get; private set; } = string.Empty;
    public static string ConnectIconPath { get; private set; } = string.Empty;
    public static string DisconnectIconPath { get; private set; } = string.Empty;
    public static string CleanIconPath { get; private set; } = string.Empty;
    public static string ClearIconPath { get; private set; } = string.Empty;
    public static string ListIconPath { get; private set; } = string.Empty;
    public static string DatabaseIconPath { get; private set; } = string.Empty;
    public static string UnpinIcon1Path { get; private set; } = string.Empty;
    public static string ErrorIconPath { get; private set; } = string.Empty;
    public static string TextIconPath { get; private set; } = string.Empty;
    public static string ImageIconPath { get; private set; } = string.Empty;
    public static string FileIconPath { get; private set; } = string.Empty;
    public static string CopyIconPath { get; private set; } = string.Empty;
    public static string PinIconPath { get; private set; } = string.Empty;
    public static string UnpinIconPath { get; private set; } = string.Empty;
    public static string DeleteIconPath { get; private set; } = string.Empty;

    #endregion

    #region Methods

    public static string GetPinIconPath(bool pinned)
    {
        return pinned ? UnpinIconPath : PinIconPath;
    }

    // TODO: PR to Flow.Launcher to add this property to PluginMetaData
    private static string GetDataDirectory(string assemblyName)
    {
        string flowDir = string.Empty;

        try
        {
            // reflection to get DataLocation.DataDirectory
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "Flow.Launcher.Infrastructure");

            if (assembly != null)
            {
                var dataLocationType = assembly.GetType("Flow.Launcher.Infrastructure.UserSettings.DataLocation");

                if (dataLocationType != null)
                {
                    var method = dataLocationType.GetMethod("DataDirectory");
                    if (method != null)
                    {
                        var dataDir = method.Invoke(null, null) as string;
                        if (!string.IsNullOrEmpty(dataDir))
                        {
                            flowDir = dataDir;
                        }
                    }
                }
            }
        }
        catch (Exception _)
        {
            // ignored
        }

        if (string.IsNullOrEmpty(flowDir))
        {
            // default: C:\Users\<username>\AppData\Roaming\FlowLauncher
            flowDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FlowLauncher");
        }

        //  <flowDir>\Settings\Plugins\<pluginName>
        return Path.Combine(flowDir, "Settings", "Plugins", assemblyName);
    }

    #endregion
}
