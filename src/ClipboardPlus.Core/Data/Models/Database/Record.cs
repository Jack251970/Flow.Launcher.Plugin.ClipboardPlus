namespace ClipboardPlus.Core.Data.Models;

public class Record
{
    public int Id { get; set; }
    public string HashId { get; set; } = string.Empty;
    public string DataMd5 { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string DisplayTitle { get; set; } = string.Empty;
    public string SenderApp { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string IconMd5 { get; set; } = string.Empty;
    public string PreviewImagePath { get; set; } = string.Empty;
    public int ContentType { get; set; }
    public int Score { get; set; }
    public int InitScore { get; set; }
    public DateTime _time;

    public string Time
    {
        get => _time.ToString("O");
        set => _time = DateTime.Parse(value);
    }

    public DateTime _create_time;

    public string CreateTime
    {
        get => _create_time.ToString("O");
        set => _create_time = DateTime.Parse(value);
    }

    public bool Pinned { get; set; }

    public static Record FromClipboardData(ClipboardData data)
    {
        var iconB64 = data.Icon.ToBase64();
        var iconMd5 = iconB64.GetMd5();
        string insertData = data.DataToString();
        var dataMd5 = insertData.GetMd5();
        var record = new Record
        {
            HashId = data.HashId,
            DataMd5 = dataMd5,
            Text = data.Text,
            DisplayTitle = data.DisplayTitle,
            SenderApp = data.SenderApp,
            IconPath = data.IconPath,
            IconMd5 = iconMd5,
            PreviewImagePath = data.PreviewImagePath,
            ContentType = (int)data.Type,
            Score = data.Score,
            InitScore = data.InitScore,
            Time = data.Time.ToString("O"),
            CreateTime = data.CreateTime.ToString("O"),
            Pinned = data.Pinned,
        };
        return record;
    }

    public static ClipboardData ToClipboardData(Record record)
    {
        var type = (CbContentType)record.ContentType;
        var clipboardData = new ClipboardData
        {
            HashId = record.HashId,
            Data = record.DataMd5,
            Text = record.Text,
            DisplayTitle = record.DisplayTitle,
            SenderApp = record.SenderApp,
            // TODO: Check need save data.
            IconPath = record.IconPath,
            Icon = record.IconMd5.ToBitmapImage(),
            Glyph = ResourceHelper.GetGlyph(type),
            PreviewImagePath = record.PreviewImagePath,
            Type = type,
            Score = record.Score,
            InitScore = record.InitScore,
            Time = record._time,
            CreateTime = record._create_time,
            Pinned = record.Pinned,
        };
        switch (type)
        {
            case CbContentType.Text:
                break;
            case CbContentType.Image:
                clipboardData.Data = record.DataMd5.ToImage();
                break;
            case CbContentType.Files:
                clipboardData.Data = record.DataMd5.Split('\n');
                break;
            default:
                break;
        }
        return clipboardData;
    }
}
