using Dapper;
using Microsoft.Data.Sqlite;
using System.Globalization;
using System.Windows.Media.Imaging;
using Xunit;
using Xunit.Abstractions;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Test;

public class DatabaseHelperTest
{
    private readonly static string _defaultIconPath = "Images/clipboard.png";

    private readonly static string _baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

    private readonly BitmapImage _defaultImage = new(new Uri(Path.Combine(_baseDirectory, _defaultIconPath), UriKind.Absolute));

    private readonly ClipboardData TestRecord =
        new("Test Data")
        {
            HashId = StringUtils.GetGuid(),
            DataType = DataType.Text,
            SenderApp = "Source.exe",
            CachedImagePath = _defaultIconPath,
            Score = 241,
            InitScore = 1,
            Pinned = false,
            CreateTime = DateTime.Now,
        };

    private readonly ITestOutputHelper _testOutputHelper;

    public DatabaseHelperTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public ClipboardData GetRandomClipboardData()
    {
        var rand = new Random();
        var type = (DataType)rand.Next(3);
        object dataContent = type switch
        {
            DataType.Text => StringUtils.RandomString(10),
            DataType.Image => _defaultImage,
            DataType.Files => new string[] { StringUtils.RandomString(10), StringUtils.RandomString(10), StringUtils.RandomString(10) },
            _ => null!
        };
        var data = new ClipboardData(dataContent)
        {
            HashId = StringUtils.GetGuid(),
            DataType = type,
            SenderApp = StringUtils.RandomString(5) + ".exe",
            CachedImagePath = _defaultIconPath,
            Score = rand.Next(1000),
            InitScore = rand.Next(1000),
            Pinned = false,
            CreateTime = DateTime.Now,
        };
        return data;
    }

    [Fact]
    public async Task TestCreateDatabase()
    {
        var helper = new DatabaseHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Shared
        );
        _testOutputHelper.WriteLine(helper.Connection.ConnectionString);
        await helper.InitializeDatabaseAsync();
        var sql = @"SELECT name from sqlite_master WHERE name IN ('record', 'assets') ORDER BY name ASC;";
        var r = helper.Connection.Query(sql).AsList();
        Assert.True(r.Count == 2 && r[0].name == "assets" && r[1].name == "record");
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
            cache: SqliteCacheMode.Shared
        );
        await helper.InitializeDatabaseAsync();
        await helper.AddOneRecordAsync(exampleTextRecord);
        var c = (await helper.GetAllRecordsAsync()).First();
        Assert.Equal(c, exampleTextRecord);
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
            cache: SqliteCacheMode.Shared
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
            var tmpRecord = GetRandomClipboardData();
            tmpRecord.CreateTime = ctime + s;
            await helper.AddOneRecordAsync(tmpRecord);
        }
        // helper.Connection.BackupDatabase(new SqliteConnection("Data Source=a.db"));

        await helper.DeleteRecordByKeepTimeAsync(type, keepTime);

        var recordsAfterDelete = await helper.GetAllRecordsAsync();
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
            cache: SqliteCacheMode.Shared
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
            cache: SqliteCacheMode.Shared
        );
        await helper.InitializeDatabaseAsync();
        await helper.AddOneRecordAsync(exampleTextRecord);
        exampleTextRecord.HashId = string.Empty;
        await helper.DeleteOneRecordAsync(exampleTextRecord);
        var c = await helper.GetAllRecordsAsync();
        Assert.Empty(c);
        await helper.CloseAsync();
    }
}
