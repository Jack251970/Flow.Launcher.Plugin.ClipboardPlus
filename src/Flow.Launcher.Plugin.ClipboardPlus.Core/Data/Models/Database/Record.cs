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
    /// </summary>
    public string DataMd5 { get; set; } = string.Empty;

    /// <summary>
    /// Display text for the data.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Display title for the data.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Sender application of the data.
    /// </summary>
    public string SenderApp { get; set; } = string.Empty;

    /// <summary>
    /// Path of the cached image for preview.
    /// </summary>
    public string PreviewImagePath { get; set; } = string.Empty;

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

    public static Record FromClipboardData(ClipboardData data)
    {
        var insertData = data.DataToString();
        var dataMd5 = insertData.GetMd5();
        var record = new Record
        {
            HashId = data.HashId,
            DataMd5 = dataMd5,
            Text = data.Text,
            Title = data.Title,
            SenderApp = data.SenderApp,
            PreviewImagePath = data.PreviewImagePath,
            DataType = (int)data.DataType,
            Score = data.Score,
            InitScore = data.InitScore,
            CreateTime = data.CreateTime.ToString("O"),
            Pinned = data.Pinned,
        };
        return record;
    }

    public static ClipboardData ToClipboardData(Record record)
    {
        var type = (DataType)record.DataType;
        var clipboardData = new ClipboardData
        {
            HashId = record.HashId,
            Data = record.DataMd5,
            Text = record.Text,
            Title = record.Title,
            SenderApp = record.SenderApp,
            PreviewImagePath = record.PreviewImagePath,
            DataType = type,
            Score = record.Score,
            InitScore = record.InitScore,
            CreateTime = record._createTime,
            Pinned = record.Pinned,
        };
        switch (type)
        {
            case Enums.DataType.Text:
                break;
            case Enums.DataType.Image:
                clipboardData.Data = record.DataMd5.ToImage();
                break;
            case Enums.DataType.Files:
                clipboardData.Data = record.DataMd5.Split('\n');
                break;
            default:
                break;
        }
        return clipboardData;
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
        return $"Record(Type: {DataType}, Title: {Title}, Text: {Text}, CreateTime: {CreateTime})";
    }
}
