using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public partial struct ClipboardData : IEquatable<ClipboardData>, IDisposable
{
    #region Public Properties

    #region Data Properties

    /// <summary>
    /// Hash id of the data, used to identify the data.
    /// </summary>
    private readonly string hashId = string.Empty;
    public required readonly string HashId
    {
        get => hashId;
        init => hashId = value;
    }

    /// <summary>
    /// Clipboard data of the record.
    /// If type is Text, the data is in string.
    /// If type is Image, the data is in BitmapSource.
    /// If type is Files, the data is in string[].
    /// Else the data is null.
    /// </summary>
    private readonly object? data;
    public readonly object? Data => data!;

    /// <summary>
    /// MD5 hash of the data, also used to identify the data.
    /// </summary>
    private readonly string dataMd5;
    public readonly string DataMd5 => dataMd5;

    /// <summary>
    /// Type of the data.
    /// </summary>
    private readonly DataType dataType;
    public readonly DataType DataType => dataType;

    /// <summary>
    /// Whether the string is encrypted.
    /// Note: Currently don't support encrypting image data.
    /// </summary>
    private readonly bool encryptData;
    public readonly bool EncryptData => encryptData && dataType != DataType.Image;

    /// <summary>
    /// Sender application of the data.
    /// </summary>
    private readonly string senderApp = string.Empty;
    public required readonly string SenderApp
    {
        get => senderApp;
        init => senderApp = value;
    }

    /// <summary>
    /// Initial score of the record for pinning feature.
    /// </summary>
    private readonly int initScore;
    public required readonly int InitScore
    {
        get => initScore;
        init => initScore = value;
    }

    /// <summary>
    /// Create time of the record.
    /// </summary>
    private readonly DateTime createTime;
    public required readonly DateTime CreateTime
    {
        get => createTime;
        init => createTime = value;
    }

    /// <summary>
    /// Path of the cached image for preview.
    /// </summary>
    public string CachedImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Text in unicode format.
    /// Note: Only used for rich text.
    /// </summary>
    public string UnicodeText { get; set; } = string.Empty;

    /// <summary>
    /// MD5 hash of the encryption key for identifying the database.
    /// </summary>
    public string EncryptKeyMd5 { get; set; } = string.Empty;

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
    public required bool Pinned;

    /// <summary>
    /// Whether the data is saved to database.
    /// </summary>
    public required bool Saved;

    #endregion

    /// <summary>
    /// Whether the data is valid.
    /// </summary>
    public readonly bool IsValid => DataToValid() != null;

    /// <summary>
    /// Maximum score for pinning feature.
    /// </summary>
    public const int MaximumScore = 1000000000;

    #endregion

    #region Constructors

    public ClipboardData(object? data, DataType dataType, bool encryptData)
    {
        this.data = data;
        this.dataType = dataType;
        this.encryptData = encryptData;
        dataMd5 = StringUtils.GetMd5(DataToString(false)!);
    }

    public ClipboardData()
    {
        data = null!;
        dataType = DataType.Other;
        encryptData = false;
        dataMd5 = string.Empty;
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
            DataType.UnicodeText => Data as string ?? string.Empty,
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
    /// Get the unicode text as string.
    /// </summary>
    /// <param name="encryptData">
    /// Whether to encrypt the data following the setting.
    /// </param>
    /// <returns>
    /// If data type is Rich Text, return the data as string.
    /// Else return null.
    /// </returns>
    public readonly string? UnicodeTextToString(bool encryptData)
    {
        var str = DataType switch
        {
            DataType.RichText => UnicodeText,
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
            DataType.UnicodeText => Icon,
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
            case DataType.UnicodeText:
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
    /// Get the unicode text as valid object.
    /// </summary>
    /// <returns>
    /// If data type is Rich Text, return the data as valid string.
    /// Else return null.
    /// </returns>
    public readonly string? UnicodeTextToValid()
    {
        var stringToCopy = UnicodeText;
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
            CachedImagePath = record.CachedImagePath,
            Pinned = record.Pinned,
            Saved = true,
            UnicodeText = StringToUnicodeText(record.UnicodeText, encrypt),
            EncryptKeyMd5 = record.EncryptKeyMd5
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
            CachedImagePath = data.CachedImagePath,
            Pinned = data.Pinned,
            Saved = saved,
            UnicodeText = StringToUnicodeText(data.UnicodeText, false),
            EncryptKeyMd5 = data.EncryptKeyMd5
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
            DataType.UnicodeText => str,
            DataType.RichText => str,
            DataType.Image => str.ToBitmapImage(),
            DataType.Files => str.Split('\n'),
            _ => null
        };
    }

    private static string StringToUnicodeText(string str, bool encryptData)
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
    /// Pin symbol in unicode.
    /// </summary>
    private const string PinUnicode = "📌";

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
            var dispSubtitle = $"{CreateTime.ToString(cultureInfo)}: {SenderApp}";
            dispSubtitle = Pinned ? $"{PinUnicode}{dispSubtitle}" : dispSubtitle;
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
                DataType.UnicodeText => Data as string,
                DataType.RichText => UnicodeText,
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
                        DataType.UnicodeText => TextScore,
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
            CachedImagePath = CachedImagePath,
            Pinned = Pinned,
            Saved = Saved,
            UnicodeText = UnicodeText,
            EncryptKeyMd5 = EncryptKeyMd5
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
            CachedImagePath = CachedImagePath,
            Pinned = pinned,
            Saved = Saved,
            UnicodeText = UnicodeText,
            EncryptKeyMd5 = EncryptKeyMd5
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

    public readonly void Dispose()
    {
        if (Data is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    #endregion

    public readonly bool DataEquals(ClipboardData b)
    {
        return DataType == b.DataType &&
            DataMd5 == b.DataMd5;
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
    public string UnicodeText { get; set; } = string.Empty;
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
            CachedImagePath = data.CachedImagePath,
            Pinned = data.Pinned,
            UnicodeText = data.UnicodeTextToString(false)!,
            EncryptKeyMd5 = data.EncryptKeyMd5
        };
    }
}
