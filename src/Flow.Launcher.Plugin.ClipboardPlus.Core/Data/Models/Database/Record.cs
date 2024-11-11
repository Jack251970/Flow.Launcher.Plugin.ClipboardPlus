namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class Record
{
    #region Public Properties

    /// <summary>
    /// Primary key of the record.
    /// </summary>
    public int Id { get; set; }

    #region Data Properties

    /// <summary>
    /// Hash id of the data, used to identify the data.
    /// </summary>
    public string HashId { get; set; } = string.Empty;

    /// <summary>
    /// MD5 hash of the data, also used to identify the data.
    /// Or base64 encoded data for storing the data.
    /// </summary>
    public string DataMd5B64 { get; set; } = string.Empty;

    /// <summary>
    /// Type of the data.
    /// </summary>
    public int DataType { get; set; }

    /// <summary>
    /// Whether the string is encrypted.
    /// </summary>
    public bool EncryptData { get; set; }

    /// <summary>
    /// Sender application of the data.
    /// </summary>
    public string SenderApp { get; set; } = string.Empty;

    /// <summary>
    /// Create time of the record.
    /// </summary>
    public DateTime createTime;
    public string CreateTime
    {
        get => createTime.ToString("O");
        set => createTime = DateTime.Parse(value);
    }

    /// <summary>
    /// Datetime score of the record for sorting.
    /// </summary>
    public int DatetimeScore { get; set; }

    /// <summary>
    /// Path of the cached image for preview.
    /// </summary>
    public string CachedImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Text in unicode format.
    /// </summary>
    public string UnicodeText { get; set; } = string.Empty;

    /// <summary>
    /// MD5 hash of the encryption key for identifying the database.
    /// </summary>
    public string EncryptKeyMd5 { get; set; } = string.Empty;

    #endregion

    #region Pin

    /// <summary>
    /// Whether the record is pinned.
    /// </summary>
    public bool Pinned { get; set; }

    #endregion

    #endregion

    #region Convert Methods

    /// <summary>
    /// Convert the clipboard data to a record for inserting.
    /// DataMd5B64 is the MD5 hash of the data.
    /// </summary>
    /// <param name="data">
    /// The clipboard data to convert.
    /// </param>
    /// <param name="needEncryptData">
    /// Whether to encrypt the data following the setting.
    /// </param>
    /// <returns>
    /// The record converted from the clipboard data.
    /// </returns>
    public static Record FromClipboardData(ClipboardData data, bool needEncryptData)
    {
        var record = new Record
        {
            HashId = data.HashId,
            DataMd5B64 = data.DataMd5,
            DataType = (int)data.DataType,
            EncryptData = needEncryptData && data.EncryptData,
            SenderApp = data.SenderApp,
            CreateTime = data.CreateTime.ToString("O"),
            DatetimeScore = GetDateTimeScore(data.CreateTime),
            CachedImagePath = data.CachedImagePath,
            Pinned = data.Pinned,
            UnicodeText = string.Empty,  // just for getting the unicode text from the database
            EncryptKeyMd5 = data.EncryptKeyMd5
        };
        return record;
    }

    // Note: Use the Flow.Launcher first version commit time as the base time for sorting.
    public static DateTime BaseDateTime = new(2020, 6, 28, 10, 24, 46, DateTimeKind.Utc);
    private static readonly int BaseDateTimeScore = GetDateTimeScore(BaseDateTime);

    public static int GetDateTimeScore(DateTime dateTime)
    {
        var ctime = new DateTimeOffset(dateTime);
        var seconds = ctime.ToUnixTimeSeconds();
        var str = seconds.ToString();
        var s = str[^9..];
        var score = Convert.ToInt32(s);
        return score - BaseDateTimeScore;
    }

    #endregion

    public static bool operator ==(Record a, Record b) => a.Equals(b);

    public static bool operator !=(Record a, Record b) => !a.Equals(b);

    public override bool Equals(object? obj)
    {
        if (obj is Record record)
        {
            return Equals(record);
        }
        return false;
    }

    public bool Equals(Record b)
    {
        return HashId == b.HashId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(HashId);
    }

    public override string ToString()
    {
        return $"Record(Type: {DataType}, DataMd5B64: {DataMd5B64}, Encrypt: {EncryptData}, CreateTime: {CreateTime})";
    }
}
