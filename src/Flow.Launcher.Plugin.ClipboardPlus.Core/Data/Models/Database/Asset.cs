namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Data.Models;

public class Asset
{
    #region Public Properties

    /// <summary>
    /// Primary key of the asset.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Base64 encoded data.
    /// </summary>
    public required string DataB64 { get; set; }

    /// <summary>
    /// Base64 encoded unicode text.
    /// </summary>
    public required string UnicodeTextB64 { get; set; }

    /// <summary>
    /// MD5 hash of the data, used to identify the asset.
    /// </summary>
    public required string HashId { get; set; }

    #endregion

    #region Convert Methods

    public static Asset FromClipboardData(ClipboardData data, bool needEncryptData)
    {
        return new Asset
        {
            DataB64 = data.DataToString(needEncryptData)!,
            UnicodeTextB64 = data.UnicodeTextToString(needEncryptData)!,
            HashId = data.HashId,
        };
    }

    #endregion

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
        return HashId == b.HashId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(HashId);
    }

    public override string ToString()
    {
        return $"Asset(DataB64: {DataB64}, HashId: {HashId})";
    }
}
