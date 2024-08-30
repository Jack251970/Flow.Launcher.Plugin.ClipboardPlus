using Dapper;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Windows.Media.Imaging;
using Xunit;
using Xunit.Abstractions;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Test;

public class DatabaseHelperTest
{
    private readonly static Random _random = new();

    private readonly static string _defaultIconPath = "Images/clipboard.png";

    private readonly static string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    private readonly static string _defaultImagePath = Path.Combine(_baseDirectory, _defaultIconPath);

    private readonly BitmapImage _defaultImage = new(new Uri(_defaultImagePath, UriKind.Absolute));

    private readonly static string _encryptKey = StringUtils.GenerateEncryptKey();

    private readonly ITestOutputHelper _testOutputHelper;

    public DatabaseHelperTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
        StringUtils.InitEncryptKey(_encryptKey);
    }

    #region GetRandomClipboardData

    public ClipboardData GetRandomClipboardData() => GetRandomClipboardData(DateTime.Now, false);

    public ClipboardData GetRandomClipboardData(DateTime? createTime = null!) => GetRandomClipboardData(createTime ?? DateTime.Now, false);

    public ClipboardData GetRandomClipboardData(bool valid = true) => GetRandomClipboardData(DateTime.Now, valid);

    public ClipboardData GetRandomClipboardData(DateTime createTime, bool valid)
    {
        var type = (DataType)_random.Next(3);
        object dataContent;
        var randStr = StringUtils.RandomString(10);
        if (valid)
        {
            dataContent = type switch
            {
                DataType.UnicodeText => StringUtils.RandomString(10),
                DataType.RichText => @"{\rtf\ansi{\fonttbl{\f0 Cascadia Mono;}}{\colortbl;\red43\green145\blue175;\red255\green255\blue255;\red0\green0\blue0;\red0\green0\blue255;}\f0 \fs19 \cf1 \cb2 \highlight2 DataType\cf3 .\cf4 " + randStr + @"}",
                DataType.Image => _defaultImage,
                DataType.Files => new string[] { _baseDirectory, _defaultImagePath },
                _ => null!
            };
        }
        else
        {
            dataContent = type switch
            {
                DataType.UnicodeText => string.Empty,
                DataType.RichText => string.Empty,
                DataType.Image => string.Empty,
                DataType.Files => new string[] { StringUtils.RandomString(10), StringUtils.RandomString(10), StringUtils.RandomString(10) },
                _ => null!
            };
        }
        var encrypt = _random.NextDouble() > 0.5;
        var data = new ClipboardData(dataContent, type, encrypt)
        {
            HashId = StringUtils.GetGuid(),
            SenderApp = StringUtils.RandomString(5) + ".exe",
            InitScore = _random.Next(1000),
            CreateTime = createTime,
            Pinned = false,
            Saved = true
        };
        if (type == DataType.RichText)
        {
            data.UnicodeText = $"DataType.{randStr}";
        }
        return data;
    }

    #endregion

    [Fact]
    public async Task TestCreateDatabase()
    {
        var helper = new DatabaseHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Private
        );
        _testOutputHelper.WriteLine(helper.Connection.ConnectionString);
        await helper.InitializeDatabaseAsync();
        var sql = @"SELECT name from sqlite_master WHERE name IN ('record', 'asset') ORDER BY name ASC;";
        var r = helper.Connection.Query(sql).AsList();
        Assert.True(r.Count == 2 && r[0].name == "asset" && r[1].name == "record");
        await helper.CloseAsync();
    }

    [Fact]
    public async Task TestInsertRecord()
    {
        // test text
        var exampleTextRecord = GetRandomClipboardData();
        var helper = new DatabaseHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Private
        );
        await helper.InitializeDatabaseAsync();
        await helper.AddOneRecordAsync(exampleTextRecord);
        var c = await helper.GetAllRecordsAsync();
        var find = false;
        foreach (var record in c)
        {
            if (record.HashId == exampleTextRecord.HashId)
            {
                find = true;
                Assert.Equal(exampleTextRecord, record);
            }
        }
        Assert.True(find);
        await helper.CloseAsync();
    }

    [Theory]
    [InlineData(0, "2023-05-28 11:35:00.1+08:00", 3)]
    [InlineData(0, "2023-05-28 11:35:00.1+08:00", 3600)]
    [InlineData(1, "2023-05-28 11:35:00.1+08:00", 24)]
    [InlineData(1, "2023-05-28 11:35:00.1+08:00", 3600)]
    [InlineData(2, "2023-05-28 11:35:00.1+08:00", 72)]
    public async Task TestDeleteRecordBefore(int type, string creatTime, int keepTime)
    {
        var helper = new DatabaseHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Private
        );
        await helper.InitializeDatabaseAsync();
        var now = DateTime.Now;
        var ctime = DateTime.ParseExact(
            creatTime,
            "yyyy-MM-dd HH:mm:ss.fzzz",
            CultureInfo.CurrentCulture
        );
        var spans = Enumerable.Range(0, 5000).Skip(12).Select(i => TimeSpan.FromHours(i));
        foreach (var s in spans)
        {
            var tmpRecord = GetRandomClipboardData(ctime + s);
            await helper.AddOneRecordAsync(tmpRecord, (str) => _testOutputHelper.WriteLine($"{str}"));
            // test dulplicated data
            if (_random.NextDouble() > 0.99)
            {
                var cloneRecord = tmpRecord.Clone();
                await helper.AddOneRecordAsync(cloneRecord, (str) => _testOutputHelper.WriteLine($"{str}"));
            }
        }
        // helper.Connection.BackupDatabase(new SqliteConnection("Data Source=a.db"));
        await helper.DeleteRecordsByKeepTimeAsync(type, keepTime);
        var recordsAfterDelete = await helper.GetAllRecordsAsync((str) => _testOutputHelper.WriteLine($"{str}"));
        foreach (var record in recordsAfterDelete.Where(r => r.DataType == (DataType)type))
        {
            var expTime = record.CreateTime + TimeSpan.FromHours(keepTime);
            if (expTime < now)
            {
                _testOutputHelper.WriteLine($"{record}");
            }
            Assert.True(expTime >= now);
        }
        await helper.CloseAsync();
    }

    [Fact]
    public async Task TestPinRecord()
    {
        var exampleTextRecord = GetRandomClipboardData();
        var helper = new DatabaseHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Private
        );
        await helper.InitializeDatabaseAsync();
        await helper.AddOneRecordAsync(exampleTextRecord);
        var c1 = (await helper.GetAllRecordsAsync()).First();
        exampleTextRecord.Pinned = !exampleTextRecord.Pinned;
        await helper.PinOneRecordAsync(exampleTextRecord);
        var c2 = (await helper.GetAllRecordsAsync()).First();
        Assert.Equal(c1.Pinned, !c2.Pinned);
        await helper.CloseAsync();
    }

    [Fact]
    public async Task TestDeleteOneRecord()
    {
        var exampleTextRecord = GetRandomClipboardData();
        var helper = new DatabaseHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Private
        );
        await helper.InitializeDatabaseAsync();
        await helper.AddOneRecordAsync(exampleTextRecord);
        await helper.DeleteOneRecordAsync(exampleTextRecord);
        var c = await helper.GetAllRecordsAsync();
        Assert.Empty(c);
        await helper.CloseAsync();
    }

    [Fact]
    public async Task TestDeleteInvalidRecord()
    {
        var helper = new DatabaseHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Private
        );
        await helper.InitializeDatabaseAsync();
        var validNum = 0;
        var invalidNum = 0;
        _testOutputHelper.WriteLine("Add Data");
        for (int i = 0; i < 10; i++)
        {
            var valid = _random.NextDouble() > 0.5;
            var exampleTextRecord = GetRandomClipboardData(valid);
            await helper.AddOneRecordAsync(exampleTextRecord);
            if (valid)
            {
                validNum++;
                _testOutputHelper.WriteLine($"Valid{validNum}: {exampleTextRecord}");
            }
            else
            {
                invalidNum++;
                _testOutputHelper.WriteLine($"Invalid{invalidNum}: {exampleTextRecord}");
            }
        }
        await helper.DeleteInvalidRecordsAsync();
        var c = await helper.GetAllRecordsAsync();
        _testOutputHelper.WriteLine("After Delete Invalid Data");
        var num = 0;
        foreach (var record in c)
        {
            num++;
            _testOutputHelper.WriteLine($"{num}: {record}");
        }
        Assert.Equal(validNum, c.Count);
    }

    [Fact]
    public async Task TestDeleteUnpinnedRecord()
    {
        var helper = new DatabaseHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Private
        );
        await helper.InitializeDatabaseAsync();
        var pinnedNum = 0;
        var unpinnedNum = 0;
        _testOutputHelper.WriteLine("Add Data");
        for (int i = 0; i < 10; i++)
        {
            var pinned = _random.NextDouble() > 0.5;
            var exampleTextRecord = GetRandomClipboardData();
            exampleTextRecord.Pinned = pinned;
            await helper.AddOneRecordAsync(exampleTextRecord);
            if (pinned)
            {
                pinnedNum++;
                _testOutputHelper.WriteLine($"Pinned{pinnedNum}: {exampleTextRecord}");
            }
            else
            {
                unpinnedNum++;
                _testOutputHelper.WriteLine($"Unpinned{unpinnedNum}: {exampleTextRecord}");
            }
        }
        await helper.DeleteUnpinnedRecordsAsync();
        var c = await helper.GetAllRecordsAsync();
        _testOutputHelper.WriteLine("After Delete Unpinned Data");
        var num = 0;
        foreach (var record in c)
        {
            num++;
            _testOutputHelper.WriteLine($"{num}: {record}");
        }
        Assert.Equal(pinnedNum, c.Count);
    }
}
