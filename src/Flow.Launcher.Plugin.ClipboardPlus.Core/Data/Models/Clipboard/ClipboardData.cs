﻿using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using Windows.ApplicationModel.DataTransfer;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public partial struct ClipboardData : IEquatable<ClipboardData>, IDisposable
{
    #region Fields

    /// <summary>
    /// Gets a <see cref="ClipboardData"/> instance representing a null value.
    /// </summary>
    public static ClipboardData NULL => new()
    {
        HashId = string.Empty,
        SenderApp = string.Empty,
        InitScore = 0,
        CreateTime = DateTime.MinValue,
        Pinned = false,
        Saved = false
    };

    /// <summary>
    /// Whether the data is valid.
    /// </summary>
    public readonly bool IsValid => DataToValid() != null;

    /// <summary>
    /// Maximum score for pinning feature.
    /// </summary>
    public const int MaximumScore = 1000000000;

    #endregion

    #region Properties

    #region Public

    #region Data Properties

    /// <summary>
    /// Hash id of the data, used to identify the data.
    /// </summary>
    public required readonly string HashId { get; init; } = string.Empty;

    /// <summary>
    /// Clipboard data of the record.
    /// If type is Text, the data is in string.
    /// If type is Image, the data is in BitmapSource.
    /// If type is Files, the data is in string[].
    /// Else the data is null.
    /// </summary>
    public readonly object? Data { get; }

    /// <summary>
    /// MD5 hash of the data, also used to identify the data.
    /// </summary>
    public readonly string DataMd5 { get; }

    /// <summary>
    /// Type of the data.
    /// </summary>
    public readonly DataType DataType { get; }

    /// <summary>
    /// Whether the data is encrypted.
    /// Note: Currently don't support encrypting image data.
    /// </summary>
    private readonly bool NeedEncryptData { get; }
    public readonly bool EncryptData => NeedEncryptData && DataType != DataType.Image;

    /// <summary>
    /// Sender application of the data.
    /// </summary>
    public required readonly string SenderApp { get; init; } = string.Empty;

    /// <summary>
    /// Initial score of the record for pinning feature.
    /// </summary>
    public required readonly int InitScore { get; init; } = 0;

    /// <summary>
    /// Create time of the record.
    /// </summary>
    public required readonly DateTime CreateTime { get; init; } = DateTime.MinValue;

    /// <summary>
    /// MD5 hash of the encryption key for identifying the database.
    /// </summary>
    public string EncryptKeyMd5 { get; set; } = string.Empty;

    /// <summary>
    /// Path of the cached image for preview.
    /// </summary>
    public string CachedImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Plain text.
    /// Note: Only used for rich text.
    /// </summary>
    public string PlainText { get; set; } = string.Empty;

    #endregion

    #region Icon & Glyph

    /// <summary>
    /// Icon representing the type of the data.
    /// </summary>
    public readonly BitmapImage Icon => ResourceHelper.GetIcon(DataType);

    /// <summary>
    /// Glyph representing the type of the data.
    /// </summary>
    public readonly GlyphInfo Glyph => ResourceHelper.GetGlyph(DataType);

    #endregion

    #region Pin & Save

    /// <summary>
    /// Whether the record is pinned.
    /// </summary>
    public required bool Pinned { get; set; } = false;

    /// <summary>
    /// Whether the data is saved to database.
    /// </summary>
    public required bool Saved { get; set; } = false;

    #endregion

    #endregion

    #region Internal

    internal ClipboardHistoryItem? ClipboardHistoryItem { get; set; } = null;

    #endregion

    #endregion

    #region Constructors

    public ClipboardData(object? data, DataType dataType, bool encryptData)
    {
        Data = data;
        DataType = dataType;
        NeedEncryptData = encryptData;
        // We need data & data type & encrypt data to calculate the data MD5.
        DataMd5 = StringUtils.GetMd5(DataToString(false)!);
    }

    public ClipboardData()
    {
        Data = null!;
        DataType = DataType.Other;
        NeedEncryptData = false;
        DataMd5 = string.Empty;
    }

    #endregion

    #region Function Methods

    #region Parse Data

    /// <summary>
    /// Get the data as string.
    /// </summary>
    /// <param name="needEncryptData">
    /// Whether to encrypt the data following the setting.
    /// </param>
    /// <returns>
    /// If data type is Text, return the data as string.
    /// If data type is Image, return the data as base64 string.
    /// If data type is Files, return the data as string.
    /// Else return null.
    /// </returns>
    public readonly string? DataToString(bool needEncryptData)
    {
        var str = DataType switch
        {
            DataType.PlainText => Data as string ?? string.Empty,
            DataType.RichText => Data as string ?? string.Empty,
            DataType.Image => Data is BitmapSource img ? img.ToBase64() : string.Empty,
            DataType.Files => (Data is string[] s ? string.Join('\n', s) : Data as string) ?? string.Empty,
            _ => null
        };
        if (needEncryptData && (!string.IsNullOrEmpty(str)) && EncryptData)
        {
            str = StringUtils.Encrypt(str, StringUtils.EncryptKey);
        }
        return str;
    }

    /// <summary>
    /// Get the plain text as string.
    /// </summary>
    /// <param name="encryptData">
    /// Whether to encrypt the data following the setting.
    /// </param>
    /// <returns>
    /// If data type is Rich Text, return the data as string.
    /// Else return null.
    /// </returns>
    public readonly string? PlainTextToString(bool encryptData)
    {
        var str = DataType switch
        {
            DataType.RichText => PlainText,
            _ => null
        };
        if (encryptData && (!string.IsNullOrEmpty(str)) && EncryptData)
        {
            str = StringUtils.Encrypt(str, StringUtils.EncryptKey);
        }
        return str;
    }

    /// <summary>
    /// Get the data as image.
    /// </summary>
    /// <returns>
    /// If data type is Text or Files, return the icon as BitmapSource.
    /// If data type is Image, return the data, cached image or icon as BitmapSource.
    /// Else return null.
    /// </returns>
    public readonly BitmapSource? DataToImage()
    {
        // If the data is not a BitmapImage, try to load the cached image.
        BitmapSource? img;
        img = Data as BitmapSource;
        if (img == null && !string.IsNullOrEmpty(CachedImagePath) && File.Exists(CachedImagePath))
        {
            img = CachedImagePath.ToImage();
        }
        // If the data is still null, return the icon.
        return DataType switch
        {
            DataType.PlainText => Icon,
            DataType.RichText => Icon,
            DataType.Image => img ?? Icon,
            DataType.Files => Icon,
            _ => null
        };
    }

    /// <summary>
    /// Get the data as valid object.
    /// </summary>
    /// <returns>
    /// If data type is Text, return the data as valid string.
    /// If data type is Image, return the data as BitmapSource.
    /// If data type is Files, return the data as string[] with valid files.
    /// Else return null.
    /// </returns>
    public readonly object? DataToValid()
    {
        switch (DataType)
        {
            case DataType.PlainText:
                var stringToCopy = Data as string;
                if (string.IsNullOrEmpty(stringToCopy))
                {
                    break;
                }
                return stringToCopy;
            case DataType.RichText:
                var richTextToCopy = Data as string;
                if (string.IsNullOrEmpty(richTextToCopy))
                {
                    break;
                }
                return richTextToCopy;
            case DataType.Image:
                if (Data is not BitmapSource imageToCopy)
                {
                    break;
                }
                return imageToCopy;
            case DataType.Files:
                if (Data is not string[] filesToCopy)
                {
                    break;
                }
                var validFiles = filesToCopy.Where(FileUtils.Exists).ToArray();
                if (validFiles.Length == 0)
                {
                    break;
                }
                return validFiles;
            default:
                break;
        }
        return null;
    }

    /// <summary>
    /// Get the plain text as valid object.
    /// </summary>
    /// <returns>
    /// If data type is Rich Text, return the data as valid string.
    /// Else return null.
    /// </returns>
    public readonly string? PlainTextToValid()
    {
        var stringToCopy = PlainText;
        if (string.IsNullOrEmpty(stringToCopy))
        {
            return null;
        }
        return stringToCopy;
    }

    #endregion

    #region Convert Methods

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
    public static ClipboardData FromRecord(Record record, int initScore)
    {
        var data = record.DataMd5B64;
        var type = (DataType)record.DataType;
        var encrypt = record.EncryptData;
        return new ClipboardData(StringToData(data, type, encrypt), type, encrypt)
        {
            HashId = record.HashId,
            SenderApp = record.SenderApp,
            InitScore = initScore,
            CreateTime = record.createTime,
            EncryptKeyMd5 = record.EncryptKeyMd5,
            CachedImagePath = record.CachedImagePath,
            Pinned = record.Pinned,
            Saved = true,
            PlainText = StringToPlainText(record.PlainText, encrypt)
        };
    }

    /// <summary>
    /// Convert the json clipboard data to a clipboard data for selecting.
    /// DataB64 is the base64 encoded data.
    /// Json data isn't encrypted for importing.
    /// </summary>
    /// <param name="data">
    /// The json clipboard data to convert.
    /// </param>
    /// <param name="saved">
    /// Whether the data is saved to database.
    /// </param>
    /// <returns>
    /// The clipboard data converted from the json clipboard data.
    /// </returns>
    public static ClipboardData FromJsonClipboardData(JsonClipboardData data, bool saved)
    {
        var type = data.DataType;
        var encrypt = data.EncryptData;
        return new ClipboardData(StringToData(data.DataB64, type, false), type, encrypt)
        {
            HashId = data.HashId,
            SenderApp = data.SenderApp,
            InitScore = 0,
            CreateTime = data.CreateTime,
            EncryptKeyMd5 = data.EncryptKeyMd5,
            CachedImagePath = data.CachedImagePath,
            Pinned = data.Pinned,
            Saved = saved,
            PlainText = StringToPlainText(data.PlainText, false)
        };
    }

    #region Private

    private static object? StringToData(string str, DataType type, bool encryptData)
    {
        if (!string.IsNullOrEmpty(str) && encryptData)
        {
            str = StringUtils.Decrypt(str, StringUtils.EncryptKey);
        }
        return type switch
        {
            DataType.PlainText => str,
            DataType.RichText => str,
            DataType.Image => str.ToBitmapImage(),
            DataType.Files => str.Split('\n'),
            _ => null
        };
    }

    private static string StringToPlainText(string str, bool encryptData)
    {
        if (string.IsNullOrEmpty(str))
        {
            return string.Empty;
        }
        if (encryptData)
        {
            str = StringUtils.Decrypt(str, StringUtils.EncryptKey);
        }
        return str;
    }

    #endregion

    #endregion

    #region Display Information

    /// <summary>
    /// Cached culture info for the data.
    /// </summary>
    private CultureInfo currentCultureInfo = CultureInfo.CurrentCulture;

    /// <summary>
    /// Pin symbol.
    /// </summary>
    private const string PinSymbol = "📌";

    /// <summary>
    /// Get display title for the data.
    /// </summary>
    /// <param name="cultureInfo">
    /// The culture info for the title.
    /// </param>
    /// <param name="filePaths">
    /// The valid file paths for the data.
    /// If null, use the data directly.
    /// </param>
    /// <returns>
    /// The display title for the data.
    /// </returns>
    private string title = null!;
    public string GetTitle(CultureInfo cultureInfo, string[]? filePaths = null)
    {
        if (title == null || currentCultureInfo != cultureInfo)
        {
            title = MyRegex().Replace(GetText(cultureInfo, filePaths).Trim(), string.Empty);
            currentCultureInfo = cultureInfo;
        }
        return title;
    }

    [GeneratedRegex("(\\r|\\n|\\t|\\v)")]
    private static partial Regex MyRegex();

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
    private bool pinned = false;
    public string GetSubtitle(CultureInfo cultureInfo)
    {
        if (subtitle == null || currentCultureInfo != cultureInfo || pinned != Pinned)
        {
            var dispSubtitle = string.IsNullOrEmpty(SenderApp) ?
                $"{CreateTime.ToString(cultureInfo)}" :
                $"{CreateTime.ToString(cultureInfo)}: {SenderApp}";
            dispSubtitle = Pinned ? $"{PinSymbol}{dispSubtitle}" : dispSubtitle;
            subtitle = dispSubtitle;
            currentCultureInfo = cultureInfo;
            pinned = Pinned;
        }
        return subtitle;
    }

    /// <summary>
    /// Get display text for the data.
    /// </summary>
    /// <param name="cultureInfo">
    /// The culture info for the text.
    /// </param>
    /// <param name="filePaths">
    /// The valid file paths for the data.
    /// If null, use the data directly.
    /// </param>
    /// <returns>
    /// The display text for the data.
    /// </returns>
    private string text = null!;
    public string GetText(CultureInfo cultureInfo, string[]? filePaths = null)
    {
        if (text == null || currentCultureInfo != cultureInfo)
        {
            text = DataType switch
            {
                DataType.PlainText => Data as string,
                DataType.RichText => PlainText,
                DataType.Image => $"Image: {CreateTime.ToString(cultureInfo)}",
                DataType.Files => (filePaths is null ? Data : filePaths) is string[] t ? string.Join("\n", t.Take(2)) + "\n..." : Data as string,
                _ => null
            } ?? string.Empty;
            currentCultureInfo = cultureInfo;
        }
        return text;
    }

    #endregion

    #region Score

    /// <summary>
    /// Cached record order for the data.
    /// </summary>
    private RecordOrder currentRecordOrder = RecordOrder.CreateTime;

    /// <summary>
    /// Score interval for different data types.
    /// </summary>
    private const int TextScore = 400000000;
    private const int ImageScore = 300000000;
    private const int FilesScore = 200000000;
    private const int OtherScore = 100000000;

    /// <summary>
    /// Get the score of the data.
    /// </summary>
    /// <param name="order">
    /// The order of the data.
    /// </param>
    /// <returns>
    /// The score of the data.
    /// </returns>
    private int score = -1;
    public int GetScore(RecordOrder recordOrder)
    {
        if (Pinned)
        {
            return MaximumScore;
        }
        else if (score == -1 || recordOrder != currentRecordOrder)
        {
            switch (recordOrder)
            {
                case RecordOrder.CreateTime:
                    score = InitScore;
                    break;
                case RecordOrder.DataType:
                    score = DataType switch
                    {
                        DataType.PlainText => TextScore,
                        DataType.RichText => TextScore,
                        DataType.Image => ImageScore,
                        DataType.Files => FilesScore,
                        _ => OtherScore,
                    };
                    break;
                case RecordOrder.SourceApplication:
                    var last = int.Min(SenderApp.Length, 10);
                    score = Encoding.UTF8.GetBytes(SenderApp[..last]).Sum(i => i);
                    break;
                default:
                    return 0;
            }
            currentRecordOrder = recordOrder;
        }
        return score;
    }

    #endregion

    #region Clone

    public readonly ClipboardData Clone()
    {
        return new ClipboardData(Data, DataType, EncryptData)
        {
            HashId = StringUtils.GetGuid(),
            SenderApp = SenderApp,
            InitScore = InitScore,
            CreateTime = CreateTime,
            EncryptKeyMd5 = EncryptKeyMd5,
            CachedImagePath = CachedImagePath,
            Pinned = Pinned,
            Saved = Saved,
            PlainText = PlainText
        };
    }

    public readonly ClipboardData Clone(bool pinned)
    {
        return new ClipboardData(Data, DataType, EncryptData)
        {
            HashId = HashId,
            SenderApp = SenderApp,
            InitScore = InitScore,
            CreateTime = CreateTime,
            EncryptKeyMd5 = EncryptKeyMd5,
            CachedImagePath = CachedImagePath,
            Pinned = pinned,
            Saved = Saved,
            PlainText = PlainText
        };
    }

    #endregion

    #endregion

    #region IEquatable

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
        return DataType == b.DataType && 
            DataMd5 == b.DataMd5 &&
            SenderApp == b.SenderApp &&
            CreateTime == b.CreateTime;
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (Data is IDisposable disposable)
            {
                disposable.Dispose();
            }
            ClipboardHistoryItem = null;
            _disposed = true;
        }
    }

    #endregion

    public readonly bool FromWindowsClipboardHistory()
    {
        return ClipboardHistoryItem != null;
    }

    public readonly bool IsNull()
    {
        return HashId == string.Empty &&
            CreateTime == DateTime.MinValue;
    }

    public readonly bool DataEquals(ClipboardData b)
    {
        return DataType == b.DataType &&
            DataMd5 == b.DataMd5;
    }

    public readonly bool RecordEquals(ClipboardData b)
    {
        return HashId == b.HashId &&
            EncryptKeyMd5 == b.EncryptKeyMd5;
    }

    public override readonly int GetHashCode()
    {
        return HashCode.Combine(DataMd5, SenderApp, DataType, CreateTime);
    }

    public override string ToString()
    {
        return $"ClipboardData(Type: {DataType}, Text: {GetText(CultureInfo.CurrentCulture, null)}, Encrypt: {EncryptData}, CreateTime: {CreateTime})";
    }
}

public class JsonClipboardData
{
    public string HashId { get; set; } = string.Empty;
    public string DataB64 { get; set; } = string.Empty;
    public string DataMd5 { get; set; } = string.Empty;
    public DataType DataType { get; set; }
    public bool EncryptData { get; set; }
    public string SenderApp { get; set; } = string.Empty;
    public DateTime CreateTime { get; set; }
    public string CachedImagePath { get; set; } = string.Empty;
    public bool Pinned { get; set; }
    public string PlainText { get; set; } = string.Empty;
    public string EncryptKeyMd5 { get; set; } = string.Empty;

    public static JsonClipboardData FromClipboardData(ClipboardData data)
    {
        // json data shouldn't be encrypted for exporting
        return new JsonClipboardData()
        {
            HashId = data.HashId,
            DataB64 = data.DataToString(false)!,
            DataMd5 = data.DataMd5,
            DataType = data.DataType,
            EncryptData = data.EncryptData,
            SenderApp = data.SenderApp,
            CreateTime = data.CreateTime,
            EncryptKeyMd5 = data.EncryptKeyMd5,
            CachedImagePath = data.CachedImagePath,
            Pinned = data.Pinned,
            PlainText = data.PlainTextToString(false)!
        };
    }
}
