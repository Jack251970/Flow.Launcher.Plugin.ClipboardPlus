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
    public required string PreviewImagePath; // actually not used for now
    public required CbContentType Type;
    public required int Score;
    public required int InitScore;
    public required DateTime Time;
    public required bool Pined;
    public required DateTime CreateTime;

    public readonly bool Equals(ClipboardData b)
    {
        return HashId == b.HashId;
    }

    public override readonly int GetHashCode()
    {
        var hashcode =
            Text.GetHashCode()
            ^ DisplayTitle.GetHashCode()
            ^ SenderApp.GetHashCode()
            ^ Type.GetHashCode();
        return hashcode;
    }

    public static bool operator ==(ClipboardData a, ClipboardData b) => a.HashId == b.HashId;

    public static bool operator !=(ClipboardData a, ClipboardData b) => a.HashId != b.HashId;

    public override readonly bool Equals(object? obj)
    {
        if (obj is ClipboardData clipboardData)
        {
            return this == clipboardData;
        }
        return false;
    }

    public readonly string GetMd5()
    {
        return DataToString().GetMd5();
    }

    public readonly string DataToString()
    {
        return Type switch
        {
            CbContentType.Text => Data as string,
            CbContentType.Image => Data is not Image im ? Icon.ToBase64() : im.ToBase64(),
            CbContentType.Files => Data is string[] t ? string.Join('\n', t) : Data as string,
            _ => throw new NotImplementedException(
                "Data to string for type not in Text, Image, Files are not implemented now."
            ),  // don't process others
        } ?? string.Empty;
    }

    public override readonly string ToString()
    {
        return $"ClipboardDate(type: {Type}, text: {Text}, ctime: {CreateTime})";
    }
}
