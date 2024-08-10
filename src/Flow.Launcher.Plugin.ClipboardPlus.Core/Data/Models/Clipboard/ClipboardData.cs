using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public struct ClipboardData : IEquatable<ClipboardData>
{
    /// <summary>
    /// Hash id of the data, used to identify the data.
    /// </summary>
    public required string HashId;

    /// <summary>
    /// Clipboard data of the record.
    /// </summary>
    public required object Data;

    /// <summary>
    /// MD5 hash of the data, also used to identify the data.
    /// </summary>
    public readonly string DataMd5 => DataToString().GetMd5();

    /// <summary>
    /// Display text for the data.
    /// </summary>
    public required string Text;

    /// <summary>
    /// Display title for the data.
    /// </summary>
    public required string Title;

    /// <summary>
    /// Sender application of the data.
    /// </summary>
    public required string SenderApp;

    /// <summary>
    /// Icon representing the type of the data.
    /// </summary>
    public readonly BitmapImage Icon => ResourceHelper.GetIcon(DataType);

    /// <summary>
    /// Glyph representing the type of the data.
    /// </summary>
    public readonly GlyphInfo Glyph => ResourceHelper.GetGlyph(DataType);

    /// <summary>
    /// Path of the cached image for preview.
    /// </summary>
    public required string PreviewImagePath;

    /// <summary>
    /// Type of the data.
    /// </summary>
    public required DataType DataType;

    /// <summary>
    /// Score of the record for ranking.
    /// </summary>
    public required int Score;

    /// <summary>
    /// Initial score of the record for pinning feature.
    /// </summary>
    public required int InitScore;

    /// <summary>
    /// Whether the record is pinned.
    /// </summary>
    public required bool Pinned;

    /// <summary>
    /// Create time of the record.
    /// </summary>
    public required DateTime CreateTime;

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

    public static bool operator ==(ClipboardData a, ClipboardData b) => a.Equals(b);

    public static bool operator !=(ClipboardData a, ClipboardData b) => !a.Equals(b);

    public override readonly bool Equals(object? obj)
    {
        if (obj is ClipboardData clipboardData)
        {
            return Equals(clipboardData);
        }
        return false;
    }

    public readonly bool Equals(ClipboardData b)
    {
        return Text == b.Text &&
            Title == b.Title &&
            SenderApp == b.SenderApp &&
            DataType == b.DataType;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(Text, Title, SenderApp, DataType);
    }

    public override readonly string ToString()
    {
        return $"ClipboardData(Type: {DataType}, Title: {Title}, Text: {Text}, CreateTime: {CreateTime})";
    }
}
