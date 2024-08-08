using Dapper;
using Microsoft.Data.Sqlite;
using System.Drawing;
using System.Globalization;
using System.Windows.Media.Imaging;
using Xunit;
using Xunit.Abstractions;

namespace ClipboardPlus.Core.Test;

public class DbHelperTest
{
    private readonly static string _defaultIconPath = "Images/clipboard.png";

    private readonly Image _defaultImage = new Bitmap(_defaultIconPath);

    private readonly ClipboardData TestRecord =
        new()
        {
            HashId = StringUtils.GetGuid(),
            Text = "Text",
            Type = DataType.Text,
            Data = "Test Data",
            SenderApp = "Source.exe",
            DisplayTitle = "Test Display Title",
            IconPath = _defaultIconPath,
            Icon = new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute)),
            Glyph = ResourceHelper.GetGlyph(DataType.Text),
            PreviewImagePath = _defaultIconPath,
            Score = 241,
            InitScore = 1,
            Time = DateTime.Now,
            Pinned = false,
            CreateTime = DateTime.Now,
        };

    private readonly ITestOutputHelper _testOutputHelper;

    public DbHelperTest(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public ClipboardData GetRandomClipboardData()
    {
        var rand = new Random();
        var type = (DataType)rand.Next(3);
        var data = new ClipboardData()
        {
            HashId = StringUtils.GetGuid(),
            Text = StringUtils.RandomString(10),
            Type = type,
            Data = StringUtils.RandomString(10),
            SenderApp = StringUtils.RandomString(5) + ".exe",
            DisplayTitle = StringUtils.RandomString(10),
            IconPath = _defaultIconPath,
            Icon = new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute)),
            Glyph = ResourceHelper.GetGlyph(type),
            PreviewImagePath = _defaultIconPath,
            Score = rand.Next(1000),
            InitScore = rand.Next(1000),
            Time = DateTime.Now,
            Pinned = false,
            CreateTime = DateTime.Now,
        };
        if (data.Type == DataType.Image)
        {
            data.Data = _defaultImage;
        }
        else if (data.Type == DataType.Files)
        {
            data.Data = new string[] { StringUtils.RandomString(10), StringUtils.RandomString(10) };
        }
        return data;
    }

    [Fact]
    public async Task TestCreateDb()
    {
        var helper = new DbHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Shared
        );
        _testOutputHelper.WriteLine(helper.Connection.ConnectionString);
        await helper.CreateDbAsync();
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
        var helper = new DbHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Shared
        );
        await helper.CreateDbAsync();
        await helper.AddOneRecordAsync(exampleTextRecord);
        var c = (await helper.GetAllRecordAsync()).First();
        await helper.CloseAsync();
        Assert.True(c == exampleTextRecord);
    }

    [Theory]
    [InlineData(0, "2023-05-28 11:35:00.1+08:00", 3)]
    [InlineData(0, "2023-05-28 11:35:00.1+08:00", 3600)]
    [InlineData(1, "2023-05-28 11:35:00.1+08:00", 24)]
    [InlineData(1, "2023-05-28 11:35:00.1+08:00", 3600)]
    [InlineData(2, "2023-05-28 11:35:00.1+08:00", 72)]
    public async Task TestDeleteRecordBefore(int type, string creatTime, int keepTime)
    {
        var helper = new DbHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Shared
        );
        await helper.CreateDbAsync();
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

        var recordsAfterDelete = await helper.GetAllRecordAsync();
        foreach (var record in recordsAfterDelete.Where(r => r.Type == (DataType)type))
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
}
