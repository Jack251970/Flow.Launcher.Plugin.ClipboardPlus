﻿using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public partial struct ClipboardData : IEquatable<ClipboardData>
{
    /// <summary>
    /// Hash id of the data, used to identify the data.
    /// </summary>
    public required string HashId;

    /// <summary>
    /// Clipboard data of the record.
    /// If type is Text, the data is in string.
    /// If type is Image, the data is in BitmapImage.
    /// If type is Files, the data is in string[].
    /// </summary>
    private readonly object data;
    public readonly object Data => data;

    /// <summary>
    /// MD5 hash of the data, also used to identify the data.
    /// </summary>
    private readonly string dataMd5;
    public readonly string DataMd5 => dataMd5;

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
    /// Pin symbol in unicode.
    /// </summary>
    private const string PinUnicode = "📌";

    public ClipboardData(object obj)
    {
        data = obj;
        dataMd5 = StringUtils.GetMd5(DataToString());
        currentCultureInfo = CultureInfo.CurrentCulture;
    }

    public ClipboardData()
    {
        data = null!;
        dataMd5 = string.Empty;
        currentCultureInfo = CultureInfo.CurrentCulture;
    }

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
        static object StringToData(DataType type, string base64)
        {
            return type switch
            {
                DataType.Text => base64,
                DataType.Image => base64.ToBitmapImage(),
                DataType.Files => base64.Split('\n'),
                _ => null!
            };
        }

        var type = (DataType)record.DataType;
        return new ClipboardData(StringToData(type, record.DataMd5B64))
        {
            HashId = record.HashId,
            SenderApp = record.SenderApp,
            CachedImagePath = record.CachedImagePath,
            DataType = type,
            Score = record.Score,
            InitScore = record.InitScore,
            CreateTime = record.createTime,
            Pinned = record.Pinned,
        };
    }

    /// <summary>
    /// Cached culture info for the data.
    /// </summary>
    private CultureInfo currentCultureInfo;

    /// <summary>
    /// Get display title for the data.
    /// </summary>
    /// <param name="cultureInfo">
    /// The culture info for the title.
    /// </param>
    /// <returns>
    /// The display title for the data.
    /// </returns>
    private string title = null!;
    public string GetTitle(CultureInfo cultureInfo)
    {
        if (title == null || currentCultureInfo != cultureInfo)
        {
            title = MyRegex().Replace(GetText(cultureInfo).Trim(), "");
            currentCultureInfo = cultureInfo;
        }
        return title;
    }

    /// <summary>
    /// Get display subtitle for the data.
    /// </summary>
    /// <param name="cultureInfo">
    /// The culture info for the subtitle.
    /// </param>
    /// <returns>
    /// The display subtitle for the data.
    /// </returns>
    private string subtitle = null!;
    public string GetSubtitle(CultureInfo cultureInfo)
    {
        if (subtitle == null || currentCultureInfo != cultureInfo)
        {
            var dispSubtitle = $"{CreateTime.ToString(cultureInfo)}: {SenderApp}";
            dispSubtitle = Pinned ? $"{PinUnicode}{dispSubtitle}" : dispSubtitle;
            subtitle = dispSubtitle;
            currentCultureInfo = cultureInfo;
        }
        return subtitle;
    }

    /// <summary>
    /// Get display text for the data.
    /// </summary>
    /// <param name="cultureInfo">
    /// The culture info for the text.
    /// </param>
    /// <returns>
    /// The display text for the data.
    /// </returns>
    private string text = null!;
    public string GetText(CultureInfo cultureInfo)
    {
        if (text == null || currentCultureInfo != cultureInfo)
        {
            text = DataType switch
            {
                DataType.Text => Data as string,
                DataType.Image => $"Image: {CreateTime.ToString(cultureInfo)}",
                DataType.Files => Data is string[] t ? string.Join("\n", t.Take(2)) + "\n..." : Data as string,
                _ => null
            } ?? string.Empty;
            currentCultureInfo = cultureInfo;
        }
        return text;
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
        return DataMd5 == b.DataMd5 &&
            SenderApp == b.SenderApp &&
            DataType == b.DataType &&
            CreateTime == b.CreateTime;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(DataMd5, SenderApp, DataType, CreateTime);
    }

    public override string ToString()
    {
        return $"ClipboardData(Type: {DataType}, Text: {GetText(CultureInfo.CurrentCulture)}, CreateTime: {CreateTime})";
    }

    [GeneratedRegex("(\\r|\\n|\\t|\\v)")]
    private static partial Regex MyRegex();
}
