using System.Windows.Media.Imaging;
using Xunit.Abstractions;
using System.Drawing;
using ClipboardPlus.Core;
using Dapper;
using Microsoft.Data.Sqlite;
using System;
using System.Globalization;

namespace ClipboardPlus.Core.Test;

public class DbHelperTest
{
    private static string _defaultIconPath = "Images/clipboard.png";

    private readonly Image _defaultImage = new Bitmap(_defaultIconPath);

    private readonly ClipboardData TestRecord =
        new()
        {
            HashId = Utils.GetGuid(),
            Text = "Text",
            Type = CbContentType.Text,
            Data = "Test Data",
            SenderApp = "Source.exe",
            DisplayTitle = "Test Display Title",
            IconPath = _defaultIconPath,
            Icon = new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute)),
            PreviewImagePath = _defaultIconPath,
            Score = 241,
            InitScore = 1,
            Time = DateTime.Now,
            Pined = false,
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
        var data = new ClipboardData()
        {
            HashId = Utils.GetGuid(),
            Text = Utils.RandomString(10),
            Type = (CbContentType)rand.Next(3),
            Data = Utils.RandomString(10),
            SenderApp = Utils.RandomString(5) + ".exe",
            DisplayTitle = Utils.RandomString(10),
            IconPath = _defaultIconPath,
            Icon = new BitmapImage(new Uri(_defaultIconPath, UriKind.RelativeOrAbsolute)),
            PreviewImagePath = _defaultIconPath,
            Score = rand.Next(1000),
            InitScore = rand.Next(1000),
            Time = DateTime.Now,
            Pined = false,
            CreateTime = DateTime.Now,
        };
        if (data.Type == CbContentType.Image)
            data.Data = _defaultImage;
        else if (data.Type == CbContentType.Files)
            data.Data = new string[] { Utils.RandomString(10), Utils.RandomString(10) };
        return data;
    }

    [Fact]
    public void TestCreateDb()
    {
        var helper = new DbHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Shared
        );
        _testOutputHelper.WriteLine(helper.Connection.ConnectionString);
        helper.CreateDb();
        var sql =
            @"SELECT name from sqlite_master WHERE name IN ('record', 'assets') ORDER BY name ASC;";
        var r = helper.Connection.Query(sql).AsList();
        Assert.True(r.Count == 2 && r[0].name == "assets" && r[1].name == "record");
        helper.Close();
    }

    [Fact]
    public async void TestInsertRecord()
    {
        // test text
        var exampleTextRecord = GetRandomClipboardData();
        var helper = new DbHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Shared
        );
        helper.CreateDb();
        helper.AddOneRecord(exampleTextRecord);
        var c = (await helper.GetAllRecord()).First();
        helper.Close();
        Assert.True(c == exampleTextRecord);
    }

    [Theory]
    [InlineData(0, "2023-05-28 11:35:00.1+08:00", 3)]
    [InlineData(0, "2023-05-28 11:35:00.1+08:00", 3600)]
    [InlineData(1, "2023-05-28 11:35:00.1+08:00", 24)]
    [InlineData(1, "2023-05-28 11:35:00.1+08:00", 3600)]
    [InlineData(2, "2023-05-28 11:35:00.1+08:00", 72)]
    public async void TestDeleteRecordBefore(int type, string creatTime, int keepTime)
    {
        var helper = new DbHelper(
            "TestDb",
            mode: SqliteOpenMode.Memory,
            cache: SqliteCacheMode.Shared
        );
        helper.CreateDb();
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
            helper.AddOneRecord(tmpRecord);
        }
        // helper.Connection.BackupDatabase(new SqliteConnection("Data Source=a.db"));

        helper.DeleteRecordByKeepTime(type, keepTime);

        var recordsAfterDelete = await helper.GetAllRecord();
        foreach (var record in recordsAfterDelete.Where(r => r.Type == (CbContentType)type))
        {
            var expTime = record.CreateTime + TimeSpan.FromHours(keepTime);
            if (expTime < now)
                _testOutputHelper.WriteLine($"{record}");
            Assert.True(expTime >= now);
        }
        helper.Close();
    }
}
