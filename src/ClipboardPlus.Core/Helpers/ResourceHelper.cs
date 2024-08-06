using System.Windows.Media.Imaging;

namespace ClipboardPlus.Core.Helpers;

public static class ResourceHelper
{
    #region Properties

    private static PluginInitContext Context = null!;

    #endregion

    #region Initialization

    private static bool IsInitialized = false;

    public static void Init(PluginInitContext context)
    {
        if (!IsInitialized)
        {
            Context = context;
            IsInitialized = true;
        }
    }

    #endregion

    #region Icons

    #region Methods

    private static readonly BitmapImage AppIcon = new(new Uri(PathHelpers.AppIconPath, UriKind.RelativeOrAbsolute));
    private static readonly BitmapImage TextIcon = new(new Uri(PathHelpers.TextIconPath, UriKind.RelativeOrAbsolute));
    private static readonly BitmapImage FilesIcon = new(new Uri(PathHelpers.FileIconPath, UriKind.RelativeOrAbsolute));
    private static readonly BitmapImage ImageIcon = new(new Uri(PathHelpers.ImageIconPath, UriKind.RelativeOrAbsolute));

    public static BitmapImage GetIcon(CbContentType type)
    {
        return type switch
        {
            CbContentType.Text => TextIcon,
            CbContentType.Files => FilesIcon,
            CbContentType.Image => ImageIcon,
            _ => AppIcon
        };
    }

    #endregion

    #endregion

    #region Glyphs

    #region Properties

    public static readonly GlyphInfo ListGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uEA37");
    public static readonly GlyphInfo DatabaseGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uEE94");
    public static readonly GlyphInfo ClearGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE894");

    public static readonly GlyphInfo CopyGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE8C8");
    public static readonly GlyphInfo DeleteGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE74D");

    #endregion

    #region Methods

    private static readonly GlyphInfo PinGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE718");
    private static readonly GlyphInfo UnpinGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE77A");

    public static GlyphInfo GetPinGlyph(bool pinned)
    {
        return pinned ? UnpinGlyph : PinGlyph;
    }

    private static readonly GlyphInfo UnknownGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE9CE");
    private static readonly GlyphInfo TextGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE8C1");
    private static readonly GlyphInfo FilesGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE7C3");
    private static readonly GlyphInfo ImageGlyph = new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE91B");

    public static GlyphInfo GetGlyph(CbContentType type)
    {
        return type switch
        {
            CbContentType.Text => TextGlyph,
            CbContentType.Files => FilesGlyph,
            CbContentType.Image => ImageGlyph,
            _ => UnknownGlyph
        };
    }

    #endregion

    #endregion

    #region Translations

    #region Properties

    public static string PluginTitle => Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_plugin_name");
    public static string PluginDescription => Context.API.GetTranslation("flowlauncher_plugin_clipboardplus_plugin_description");

    #endregion

    #region Methods

    private static string GetTranslation(string key) => Context.API.GetTranslation(key);

    public static string GetPinTitleTranslation(bool pinned)
    {
        return GetTranslation(pinned ? "flowlauncher_plugin_clipboardplus_unpin_title" : "flowlauncher_plugin_clipboardplus_pin_title");
    }

    public static string GetPinSubtitleTranslation(bool pinned)
    {
        return GetTranslation(pinned ? "flowlauncher_plugin_clipboardplus_unpin_subtitle" : "flowlauncher_plugin_clipboardplus_pin_subtitle");
    }

    #endregion

    #endregion
}
