using System.Windows.Media.Imaging;

namespace ClipboardPlus.Core.Helpers;

/// <summary>
/// Helper class for resources.
/// It is recommended to generate the new instances of all resources.
/// </summary>
public static class ResourceHelper
{
    #region Icons

    #region Methods

    private static BitmapImage AppIcon => new(new Uri(PathHelpers.AppIconPath, UriKind.RelativeOrAbsolute));
    private static BitmapImage TextIcon => new(new Uri(PathHelpers.TextIconPath, UriKind.RelativeOrAbsolute));
    private static BitmapImage FilesIcon => new(new Uri(PathHelpers.FileIconPath, UriKind.RelativeOrAbsolute));
    private static BitmapImage ImageIcon => new(new Uri(PathHelpers.ImageIconPath, UriKind.RelativeOrAbsolute));

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

    public static GlyphInfo ListGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uEA37");
    public static GlyphInfo DatabaseGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uEE94");
    public static GlyphInfo ClearGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE894");

    public static GlyphInfo CopyGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE8C8");
    public static GlyphInfo DeleteGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE74D");

    #endregion

    #region Methods

    private static GlyphInfo PinGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE718");
    private static GlyphInfo UnpinGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE77A");

    public static GlyphInfo GetPinGlyph(bool pinned)
    {
        return pinned ? UnpinGlyph : PinGlyph;
    }

    private static GlyphInfo UnknownGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE9CE");
    private static GlyphInfo TextGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE8C1");
    private static GlyphInfo FilesGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE7C3");
    private static GlyphInfo ImageGlyph => new(FontFamily: "/Resources/#Segoe Fluent Icons", Glyph: "\uE91B");

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
}
