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
    public readonly string DataMd5 => StringUtils.GetMd5(DataToString());

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
    public required string CachedImagePath;

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

    /// <summary>
    /// Get the data as string. If the data is not in Text, Image, Files, return empty string.
    /// </summary>
    /// <returns>
    /// If data is in Text, return the data as string.
    /// If data is in Image, return the data as base64 string.
    /// If data is in Files, return the data as string.
    /// </returns>
    public readonly string DataToString()
    {
        return DataType switch
        {
            DataType.Text => Data as string,
            DataType.Image => Data is not BitmapImage img ? Icon.ToBase64() : img.ToBase64(),
            DataType.Files => Data is string[] t ? string.Join('\n', t) : Data as string,
            _ => null
        } ?? string.Empty;
    }

    /// <summary>
    /// Get the data as image. If the data is not in Image, return null.
    /// </summary>
    /// <returns>
    /// If data is in Image, return the data as BitmapImage.
    /// If data is not in Image, return null.
    /// </returns>
    public readonly BitmapImage? DataToImage()
    {
        return DataType switch
        {
            DataType.Text => null,
            DataType.Image => Data as BitmapImage,
            DataType.Files => null,
            _ => null
        };
    }

    /// <summary>
    /// Convert the record to a clipboard data for selecting.
    /// DataMd5B64 is the base64 encoded data.
    /// </summary>
    /// <param name="data">
    /// The record to convert.
    /// </param>
    /// <returns>
    /// The clipboard data converted from the record.
    /// </returns>
    public static ClipboardData FromRecord(Record record)
    {
        var type = (DataType)record.DataType;
        var clipboardData = new ClipboardData
        {
            HashId = record.HashId,
            Data = null!,
            Text = record.Text,
            Title = record.Title,
            SenderApp = record.SenderApp,
            CachedImagePath = record.CachedImagePath,
            DataType = type,
            Score = record.Score,
            InitScore = record.InitScore,
            CreateTime = record._createTime,
            Pinned = record.Pinned,
        };
        switch (type)
        {
            case DataType.Text:
                clipboardData.Data = record.DataMd5B64;
                break;
            case DataType.Image:
                clipboardData.Data = record.DataMd5B64.ToBitmapImage();
                break;
            case DataType.Files:
                clipboardData.Data = record.DataMd5B64.Split('\n');
                break;
            default:
                break;
        }
        return clipboardData;
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
