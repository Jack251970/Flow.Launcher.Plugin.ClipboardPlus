using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public partial struct ClipboardData : IEquatable<ClipboardData>
{
    #region Public Properties

    /// <summary>
    /// Hash id of the data, used to identify the data.
    /// </summary>
    public required string HashId;

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
    private readonly DataType dataType;
    public readonly DataType DataType => dataType;

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
    /// Whether the string is encrypted.
    /// Note: Currently don't support encrypting image data.
    /// </summary>
    private readonly bool encryptData;
    public readonly bool EncryptData => encryptData;

    /// <summary>
    /// Whether the data is valid.
    /// </summary>
    public readonly bool IsValid => DataToValid() != null;

    /// <summary>
    /// Maximum score for pinning feature.
    /// </summary>
    private const int MaxScore = int.MaxValue - 1;

    #endregion

    #region Constructors

    public ClipboardData(object? data, DataType dataType, bool encryptData)
    {
        this.data = data;
        this.dataType = dataType;
        this.encryptData = dataType != DataType.Image && encryptData;
        dataMd5 = StringUtils.GetMd5(DataToString(true)!);
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
    /// <param name="encrypt">
    /// Whether to encrypt the data.
    /// </param>
    /// <returns>
    /// If data type is Text, return the data as string.
    /// If data type is Image, return the data as base64 string.
    /// If data type is Files, return the data as string.
    /// Else return null.
    /// </returns>
    public readonly string? DataToString(bool encrypt)
    {
        var str = DataType switch
        {
            DataType.Text => Data as string ?? string.Empty,
            DataType.Image => Data is BitmapSource img ? img.ToBase64() : string.Empty,
            DataType.Files => (Data is string[] s ? string.Join('\n', s) : Data as string)?? string.Empty,
            _ => null
        };
        if (!string.IsNullOrEmpty(str) && encrypt && EncryptData)
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
            img = new BitmapImage(new Uri(CachedImagePath, UriKind.Absolute));
        }
        // If the data is still null, return the icon.
        return DataType switch
        {
            DataType.Text => Icon,
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
            case DataType.Text:
                var stringToCopy = Data as string;
                if (string.IsNullOrEmpty(stringToCopy))
                {
                    break;
                }
                return stringToCopy;
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
    public static ClipboardData FromRecord(Record record)
    {
        static object? StringToData(string str, DataType type, bool encrypt)
        {
            if (!string.IsNullOrEmpty(str) && encrypt)
            {
                str = StringUtils.Decrypt(str, StringUtils.EncryptKey);
            }
            return type switch
            {
                DataType.Text => str,
                DataType.Image => str.ToBitmapImage(),
                DataType.Files => str.Split('\n'),
                _ => null
            };
        }

        var data = record.DataMd5B64;
        var type = (DataType)record.DataType;
        var encrypt = record.EncryptData;
        return new ClipboardData(StringToData(data, type, encrypt), type, encrypt)
        {
            HashId = record.HashId,
            SenderApp = record.SenderApp,
            CachedImagePath = record.CachedImagePath,
            InitScore = record.InitScore,
            CreateTime = record.createTime,
            Pinned = record.Pinned,
        };
    }

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

    #endregion

    #endregion

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
}
