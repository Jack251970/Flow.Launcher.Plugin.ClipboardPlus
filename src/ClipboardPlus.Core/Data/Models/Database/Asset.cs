namespace ClipboardPlus.Core.Data.Models;

public class Asset
{
    /// <summary>
    /// Primary key of the asset.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Base64 encoded data, used to store the asset.
    /// </summary>
    public string? DataB64 { get; set; }

    /// <summary>
    /// MD5 hash of the data, used to identify the asset.
    /// </summary>
    public string? DataMd5 { get; set; }
}
