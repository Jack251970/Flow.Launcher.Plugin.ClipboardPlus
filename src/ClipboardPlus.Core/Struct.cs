using System.Windows.Media.Imaging;

namespace ClipboardPlus.Core;

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

    public bool Equals(ClipboardData b)
    {
        return HashId == b.HashId;
    }

    public override int GetHashCode()
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

    public string GetMd5()
    {
        return DataToString().GetMd5();
    }

    public string DataToString()
    {
        string? dataString;
        switch (Type)
        {
            case CbContentType.Text:
                dataString = Data as string;
                break;
            case CbContentType.Image:
                dataString = Data is not Image im ? Icon.ToBase64() : im.ToBase64();
                break;
            case CbContentType.Files:
                dataString = Data is string[] t ? string.Join('\n', t) : Data as string;
                break;
            default:
                // don't process others
                throw new NotImplementedException(
                    "Data to string for type not in Text, Image, Files are not implemented now."
                );
        }

        return dataString ?? "";
    }

    public override string ToString()
    {
        return $"ClipboardDate(type: {Type}, text: {Text}, ctime: {CreateTime})";
    }
}
