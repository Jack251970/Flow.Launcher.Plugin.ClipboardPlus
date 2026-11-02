// ImagePersistenceTests.cs
using Dapper;
using Microsoft.Data.Sqlite;
using System.Reflection;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Test;

[Collection("Bulk insert isolation")]
public class ImagePersistenceTests
{
    [Fact]
    public async Task CapturedEncodingMatchesLegacyStorageAndSurvivesQueuedWrite()
    {
        StringUtils.InitEncryptKey(StringUtils.GenerateEncryptKey());
        var image = CreateImage();
        image.Freeze();
        var captured = CreateRecord(image, true, out var encoded);
        Assert.NotNull(encoded);
        Assert.False(captured.EncryptData); // Existing behavior: images are never encrypted.
        Assert.Equal(StringUtils.GetMd5(encoded), captured.DataMd5);
        Assert.Equal(Asset.FromClipboardData(captured, true).DataB64, encoded);

        await using var database = CreateDatabase();
        await database.InitializeDatabaseAsync();
        var queueGate = (SemaphoreSlim)typeof(SqliteDatabase).GetField("_queueSemaphore", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(database)!;
        await queueGate.WaitAsync();
        Task write;
        try
        {
            write = database.AddOneRecordWithEncodedImageAsync(captured, encoded!);
            Assert.False(write.IsCompleted);
        }
        finally
        {
            queueGate.Release();
        }
        await write;
        Assert.Equal(encoded, database.Connection.QuerySingle<string>("SELECT data_b64 FROM asset WHERE hash_id='image'"));
        var loaded = Assert.Single(await database.GetAllRecordsAsync(true));
        Assert.Equal(captured.DataMd5, loaded.DataMd5);
        var restored = Assert.IsAssignableFrom<BitmapSource>(loaded.Data);
        Assert.Equal(image.PixelWidth, restored.PixelWidth);
        Assert.Equal(image.PixelHeight, restored.PixelHeight);
        var originalPixels = new byte[8];
        var restoredPixels = new byte[8];
        image.CopyPixels(originalPixels, 8, 0);
        new FormatConvertedBitmap(restored, PixelFormats.Bgra32, null, 0).CopyPixels(restoredPixels, 8, 0);
        Assert.Equal(originalPixels, restoredPixels);
    }

    [Fact]
    public async Task FailedWriteDoesNotRetainTheQueueAndLaterWriteSucceeds()
    {
        var image = CreateImage();
        image.Freeze();
        var data = CreateRecord(image, false, out var encoded);
        await using var database = CreateDatabase();
        await database.InitializeDatabaseAsync();
        database.Connection.Execute("CREATE TRIGGER fail_image BEFORE INSERT ON asset BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;");
        await Assert.ThrowsAsync<SqliteException>(() => database.AddOneRecordWithEncodedImageAsync(data, encoded!));
        database.Connection.Execute("DROP TRIGGER fail_image");
        await database.AddOneRecordWithEncodedImageAsync(data, encoded!);
        Assert.Equal(encoded, database.Connection.QuerySingle<string>("SELECT data_b64 FROM asset WHERE hash_id='image'"));
    }

    [Fact]
    public void MutableImagesDoNotProduceReusableEncodings()
    {
        var image = CreateImage();
        Assert.False(image.IsFrozen);
        var record = CreateRecord(image, false, out var encoded);
        Assert.Null(encoded);
        Assert.NotEmpty(record.DataMd5);
    }

    private static BitmapSource CreateImage() => BitmapSource.Create(2, 1, 96, 96, PixelFormats.Bgra32, null,
        new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 }, 8);

    private static SqliteDatabase CreateDatabase() =>
        new("image-persistence-test", new ClipboardPlus(), SqliteCacheMode.Private, SqliteOpenMode.Memory);

    private static ClipboardData CreateRecord(BitmapSource image, bool encrypt, out string? encoded) => new(image, DataType.Image, encrypt, out encoded)
    {
        HashId = "image",
        SenderApp = "synthetic.exe",
        InitScore = 0,
        CreateTime = new DateTime(2025, 1, 1),
        EncryptKeyMd5 = StringUtils.EncryptKeyMd5,
        CachedImagePath = string.Empty,
        Pinned = false,
        Saved = true
    };
}
