namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class Asset
{
    /// <summary>
    /// Primary key of the asset.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Base64 encoded data, used to store the asset.
    /// </summary>
    public required string DataB64 { get; set; }

    /// <summary>
    /// MD5 hash of the data, used to identify the asset.
    /// </summary>
    public required string DataMd5 { get; set; }

    public static Asset FromClipboardData(ClipboardData data)
    {
        var dataB64 = data.DataToString();
        var dataMd5 = StringUtils.GetMd5(dataB64);
        return new Asset
        {
            DataB64 = dataB64,
            DataMd5 = dataMd5,
        };
    }

    public static bool operator ==(Asset a, Asset b) => a.Equals(b);

    public static bool operator !=(Asset a, Asset b) => !a.Equals(b);

    public override bool Equals(object? obj)
    {
        if (obj is Asset asset)
        {
            return Equals(asset);
        }
        return false;
    }

    public bool Equals(Asset b)
    {
        return DataMd5 == b.DataMd5;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(DataMd5);
    }

    public override string ToString()
    {
        return $"Asset(DataMd5: {DataMd5})";
    }
}
