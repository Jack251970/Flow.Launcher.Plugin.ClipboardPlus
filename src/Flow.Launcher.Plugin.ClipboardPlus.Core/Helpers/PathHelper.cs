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

    public static void Init(PluginInitContext context)
    {
        if (!IsInitialized)
        {
            // plugin paths
            PluginPath = context.CurrentPluginMetadata.PluginDirectory;
            ImageCachePath = Path.Combine(PluginPath, "CachedImages");
            if (!Directory.Exists(ImageCachePath))
            {
                Directory.CreateDirectory(ImageCachePath);
            }
            FileUtils.ClearImageCache(ImageCachePath, TempCacheImageName);
            IconPath = Path.Combine(PluginPath, "Images");

            // data paths
            SettingsPath = Path.Combine(PluginPath, SettingsFile);
            DatabasePath = Path.Combine(PluginPath, DatabaseFile);

            // icons paths
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

    #endregion
}
