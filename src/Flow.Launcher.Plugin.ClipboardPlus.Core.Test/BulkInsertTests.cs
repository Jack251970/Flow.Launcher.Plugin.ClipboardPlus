// BulkInsertTests.cs
using Dapper;
using Microsoft.Data.Sqlite;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Xunit;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Test;

// The encryption key is process-wide and other database tests change it.
[CollectionDefinition("Bulk insert isolation", DisableParallelization = true)]
public class BulkInsertCollection;

[Collection("Bulk insert isolation")]
public class BulkInsertTests
{
    [Fact]
    public async Task BulkInsertRoundTripsMixedRecordsAndIgnoresDuplicateIds()
    {
        StringUtils.InitEncryptKey(StringUtils.GenerateEncryptKey());
        await using var database = CreateDatabase();
        await database.InitializeDatabaseAsync();
        var pixels = new byte[] { 10, 20, 30, 255, 40, 50, 60, 255 };
        var image = BitmapSource.Create(2, 1, 96, 96, PixelFormats.Bgra32, null, pixels, 8);
        image.Freeze();
        var records = new[]
        {
            CreateRecord("plain", "synthetic text", DataType.PlainText, false),
            CreateRecord("encrypted", "synthetic secret", DataType.PlainText, true),
            CreateRecord("image", image, DataType.Image, false),
            CreateRecord("plain", "duplicate must not overwrite", DataType.PlainText, false)
        };

        await database.AddRecordsAsync(records, true);
        var loaded = await database.GetAllRecordsAsync(true);

        Assert.Equal(3, loaded.Count);
        Assert.Equal("synthetic text", Assert.Single(loaded, item => item.HashId == "plain").Data);
        Assert.Equal("synthetic secret", Assert.Single(loaded, item => item.HashId == "encrypted").Data);
        var restoredImage = Assert.IsAssignableFrom<BitmapSource>(Assert.Single(loaded, item => item.HashId == "image").Data);
        Assert.Equal(2, restoredImage.PixelWidth);
        Assert.Equal(1, restoredImage.PixelHeight);
        Assert.Equal(3, database.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM asset"));
        Assert.NotEqual("synthetic secret", database.Connection.QuerySingle<string>("SELECT data_b64 FROM asset WHERE hash_id='encrypted'"));
    }

    [Fact]
    public async Task FailedBatchRollsBackBothTablesAndAllowsLaterWrites()
    {
        StringUtils.InitEncryptKey(StringUtils.GenerateEncryptKey());
        await using var database = CreateDatabase();
        await database.InitializeDatabaseAsync();
        database.Connection.Execute("""
            CREATE TRIGGER fail_synthetic_record BEFORE INSERT ON record
            WHEN NEW.hash_id = 'fail'
            BEGIN SELECT RAISE(ABORT, 'synthetic failure'); END;
            """);
        var records = new[]
        {
            CreateRecord("first", "first synthetic text", DataType.PlainText, false),
            CreateRecord("fail", "second synthetic text", DataType.PlainText, false)
        };

        await Assert.ThrowsAsync<SqliteException>(() => database.AddRecordsAsync(records, true));
        Assert.Equal(0, database.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM record"));
        Assert.Equal(0, database.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM asset"));

        database.Connection.Execute("DROP TRIGGER fail_synthetic_record");
        await database.AddRecordsAsync(records, true);
        Assert.Equal(2, database.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM record"));
        Assert.Equal(2, database.Connection.ExecuteScalar<int>("SELECT COUNT(*) FROM asset"));
    }

    private static SqliteDatabase CreateDatabase() =>
        new("bulk-insert-test", new ClipboardPlus(), SqliteCacheMode.Private, SqliteOpenMode.Memory);

    private static ClipboardData CreateRecord(string id, object content, DataType type, bool encrypt) => new(content, type, encrypt)
    {
        HashId = id,
        SenderApp = "synthetic.exe",
        InitScore = 0,
        CreateTime = new DateTime(2025, 1, 1),
        EncryptKeyMd5 = StringUtils.EncryptKeyMd5,
        CachedImagePath = string.Empty,
        Pinned = false,
        Saved = true
    };
}
