namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class Record
{
    /// <summary>
    /// Primary key of the record.
    /// </summary>
    public int Id { get; set; }

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
    /// Sender application of the data.
    /// </summary>
    public string SenderApp { get; set; } = string.Empty;

    /// <summary>
    /// Path of the cached image for preview.
    /// </summary>
    public string CachedImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Type of the data.
    /// </summary>
    public int DataType { get; set; }

    /// <summary>
    /// Score of the record for ranking.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Initial score of the record for pinning feature.
    /// </summary>
    public int InitScore { get; set; }

    /// <summary>
    /// Whether the record is pinned.
    /// </summary>
    public bool Pinned { get; set; }

    /// <summary>
    /// Create time of the record.
    /// </summary>
    public DateTime _createTime;
    public string CreateTime
    {
        get => _createTime.ToString("O");
        set => _createTime = DateTime.Parse(value);
    }

    /// <summary>
    /// Convert the clipboard data to a record for inserting.
    /// DataMd5B64 is the MD5 hash of the data.
    /// </summary>
    /// <param name="data">
    /// The clipboard data to convert.
    /// </param>
    /// <returns>
    /// The record converted from the clipboard data.
    /// </returns>
    public static Record FromClipboardData(ClipboardData data)
    {
        var record = new Record
        {
            HashId = data.HashId,
            DataMd5B64 = data.DataMd5,
            SenderApp = data.SenderApp,
            CachedImagePath = data.CachedImagePath,
            DataType = (int)data.DataType,
            Score = data.Score,
            InitScore = data.InitScore,
            CreateTime = data.CreateTime.ToString("O"),
            Pinned = data.Pinned,
        };
        return record;
    }

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
        return $"Record(Type: {DataType}, DataMd5B64: {DataMd5B64}, CreateTime: {CreateTime})";
    }
}
