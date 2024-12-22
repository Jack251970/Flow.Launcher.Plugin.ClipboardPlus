namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Contracts;

public interface ISettings
{
    /// <summary>
    /// Action keyword to clear records
    /// </summary>
    public string ClearKeyword { get; set; }

    /// <summary>
    /// Maximum number of records to keep
    /// </summary>
    public int MaxRecords { get; set; }

    /// <summary>
    /// Order of records in query list
    /// </summary>
    public RecordOrder RecordOrder { get; set; }

    /// <summary>
    /// Action to perform when clicking on a record
    /// </summary>
    public ClickAction ClickAction { get; set; }

    /// <summary>
    /// Default copy option for rich text
    /// </summary>
    public DefaultRichTextCopyOption DefaultRichTextCopyOption { get; set; }

    /// <summary>
    /// Default copy option for images
    /// </summary>
    public DefaultImageCopyOption DefaultImageCopyOption { get; set; }

    /// <summary>
    /// Default copy option for files
    /// </summary>
    public DefaultFilesCopyOption DefaultFilesCopyOption { get; set; }

    /// <summary>
    /// Whether to put action items on the top of query list
    /// </summary>
    public bool ActionTop { get; set; }

    /// <summary>
    /// Whether to cache images in local folder
    /// </summary>
    public bool CacheImages { get; set; }

    /// <summary>
    /// Format of the cache image file name
    /// </summary>
    public string CacheFormat { get; set; }

    /// <summary>
    /// Whether to encrypt records
    /// </summary>
    public bool EncryptData { get; set; }

    /// <summary>
    /// Key to use for encryption
    /// </summary>
    public string EncryptKey { get; set; }

    /// <summary>
    /// Whether to keep text records
    /// </summary>
    public bool KeepText { get; set; }

    /// <summary>
    /// Time to keep text records
    /// </summary>
    public KeepTime TextKeepTime { get; set; }

    /// <summary>
    /// Whether to keep image records
    /// </summary>
    public bool KeepImages { get; set; }

    /// <summary>
    /// Time to keep image records
    /// </summary>
    public KeepTime ImagesKeepTime { get; set; }

    /// <summary>
    /// Whether to keep file records
    /// </summary>
    public bool KeepFiles { get; set; }

    /// <summary>
    /// Time to keep file records
    /// </summary>
    public KeepTime FilesKeepTime { get; set; }

    /// <summary>
    /// List of data type and keep time pairs
    /// </summary>
    public List<Tuple<DataType, KeepTime>> KeepTimePairs { get; }
}
