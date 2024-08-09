using System.Windows.Media.Imaging;

namespace ClipboardPlus.Core.Data.Models;

public struct ClipboardData : IEquatable<ClipboardData>
{
    public required string HashId;
    public required object Data;
    public required string Text;
    public required string DisplayTitle;
    public required string SenderApp;
    public required string IconPath;
    public required BitmapImage Icon;
    public required GlyphInfo Glyph;
    public required string PreviewImagePath; // actually not used for now
    public required DataType DataType;
    public required int Score;
    public required int InitScore;
    public required DateTime Time;
    public required bool Pinned;
    public required DateTime CreateTime;

    public static bool operator ==(ClipboardData a, ClipboardData b) => a.Equals(b);

    public static bool operator !=(ClipboardData a, ClipboardData b) => !a.Equals(b);

    public readonly bool Equals(ClipboardData b)
    {
        return Equals(b);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is ClipboardData clipboardData)
        {
            return HashId == clipboardData.HashId;
        }
        return false;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Text, DisplayTitle, SenderApp, DataType);
    }

    public readonly string GetMd5()
    {
        return DataToString().GetMd5();
    }

    public readonly string DataToString()
    {
        return DataType switch
        {
            DataType.Text => Data as string,
            DataType.Image => Data is not Image im ? Icon.ToBase64() : im.ToBase64(),
            DataType.Files => Data is string[] t ? string.Join('\n', t) : Data as string,
            _ => throw new NotImplementedException(
                "Data to string for type not in Text, Image, Files are not implemented now."
            ),  // don't process others
        } ?? string.Empty;
    }

    public readonly Image? DataToImage()
    {
        return DataType switch
        {
            DataType.Text => null,
            DataType.Image => Data as Image,
            DataType.Files => null,
            _ => throw new NotImplementedException(
                "Data to string for type not in Text, Image, Files are not implemented now."
            ),  // don't process others
        };
    }

    public override readonly string ToString()
    {
        return $"ClipboardDate(type: {DataType}, text: {Text}, ctime: {CreateTime})";
    }
}
