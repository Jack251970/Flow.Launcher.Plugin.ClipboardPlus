using System.Windows.Media.Imaging;

namespace ClipboardPlus.Core.Helpers;

public static class ResourceHelper
{
    #region Icons

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

    #region Glyphs

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
}
