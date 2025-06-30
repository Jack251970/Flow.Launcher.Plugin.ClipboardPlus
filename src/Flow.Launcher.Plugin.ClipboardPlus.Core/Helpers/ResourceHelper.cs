using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

/// <summary>
/// Helper class for resources.
/// It is recommended to generate the new instances of all resources.
/// </summary>
public static class ResourceHelper
{
    #region Icons

    #region Methods

    // Warning: Only Absolute Uri is supported.
    private static BitmapImage AppIcon => new(new Uri(PathHelper.AppIconPath, UriKind.Absolute));
    private static BitmapImage TextIcon => new(new Uri(PathHelper.TextIconPath, UriKind.Absolute));
    private static BitmapImage FilesIcon => new(new Uri(PathHelper.FileIconPath, UriKind.Absolute));
    private static BitmapImage ImageIcon => new(new Uri(PathHelper.ImageIconPath, UriKind.Absolute));

    public static BitmapImage GetIcon(DataType type)
    {
        return type switch
        {
            DataType.PlainText => TextIcon,
            DataType.RichText => TextIcon,
            DataType.Files => FilesIcon,
            DataType.Image => ImageIcon,
            _ => AppIcon
        };
    }

    #endregion

    #endregion

    #region Glyphs

    #region Properties

    public static GlyphInfo ConnectGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE703");
    public static GlyphInfo DisconnectGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE703");
    public static GlyphInfo CleanGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE894");
    public static GlyphInfo ClearHistoryGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uF0E3");
    public static GlyphInfo ClearGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE894");
    public static GlyphInfo ListGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uEA37");
    public static GlyphInfo DatabaseGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uEE94");
    public static GlyphInfo UnpinGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE77A");
    public static GlyphInfo ErrorGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE783");

    public static GlyphInfo CopyGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE8C8");
    public static GlyphInfo DeleteGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE74D");

    #endregion

    #region Methods

    private static GlyphInfo PinGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE718");

    public static GlyphInfo GetPinGlyph(bool pinned)
    {
        return pinned ? UnpinGlyph : PinGlyph;
    }

    private static GlyphInfo UnknownGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE9CE");
    private static GlyphInfo TextGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE8C1");
    private static GlyphInfo FilesGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE7C3");
    private static GlyphInfo ImageGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE91B");

    public static GlyphInfo GetGlyph(DataType type)
    {
        return type switch
        {
            DataType.PlainText => TextGlyph,
            DataType.RichText => TextGlyph,
            DataType.Files => FilesGlyph,
            DataType.Image => ImageGlyph,
            _ => UnknownGlyph
        };
    }

    #endregion

    #endregion

    #region Strings

    public static string GetString(IClipboardPlus clipboardPlus, DataType type)
    {
        return type switch
        {
            DataType.PlainText => clipboardPlus.Context.GetTranslation("flowlauncher_plugin_clipboardplus_plain_text"),
            DataType.RichText => clipboardPlus.Context.GetTranslation("flowlauncher_plugin_clipboardplus_rich_text"),
            DataType.Files => clipboardPlus.Context.GetTranslation("flowlauncher_plugin_clipboardplus_files"),
            DataType.Image => clipboardPlus.Context.GetTranslation("flowlauncher_plugin_clipboardplus_image"),
            _ => clipboardPlus.Context.GetTranslation("flowlauncher_plugin_clipboardplus_others")
        };
    }

    #endregion
}
