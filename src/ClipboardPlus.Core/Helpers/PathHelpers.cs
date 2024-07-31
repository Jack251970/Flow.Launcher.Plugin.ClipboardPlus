namespace ClipboardPlus.Core.Helpers;

public static class PathHelpers
{
    private const string SettingsFile = "settings.json";
    private const string DatabaseFile = "ClipboardPlus.db";

    // plugin paths
    public static string PluginPath { get; private set; } = string.Empty;
    public static string ImageCachePath { get; private set; } = string.Empty;
    public static string IconPath { get; private set; } = string.Empty;

    // data paths
    public static string SettingsPath { get; private set; } = string.Empty;
    public static string DatabasePath { get; private set; } = string.Empty;

    // icons paths
    public static string AppIconPath { get; private set; } = string.Empty;
    public static string PinnedIconPath { get; private set; } = string.Empty;
    public static string ClearIconPath { get; private set; } = string.Empty;
    public static string ListIconPath { get; private set; } = string.Empty;
    public static string DatabaseIconPath { get; private set; } = string.Empty;
    public static string TextIconPath { get; private set; } = string.Empty;
    public static string ImageIconPath { get; private set; } = string.Empty;
    public static string FileIconPath { get; private set; } = string.Empty;

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
            IconPath = Path.Combine(IconPath);

            // data paths
            SettingsPath = Path.Combine(PluginPath, SettingsFile);
            DatabasePath = Path.Combine(PluginPath, DatabaseFile);

            // icons paths
            AppIconPath = Path.Combine(IconPath, "clipboard.png");
            PinnedIconPath = Path.Combine(IconPath, "pinned.png");
            ClearIconPath = Path.Combine(IconPath, "clear.png");
            ListIconPath = Path.Combine(IconPath, "list.png");
            DatabaseIconPath = Path.Combine(IconPath, "database.png");
            TextIconPath = Path.Combine(IconPath, "text.png");
            ImageIconPath = Path.Combine(IconPath, "image.png");
            FileIconPath = Path.Combine(IconPath, "file.png");
            IsInitialized = true;
        }
    }
}
