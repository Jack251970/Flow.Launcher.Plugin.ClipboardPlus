using System.Data;
using Dapper;
using Microsoft.Data.Sqlite;

namespace ClipboardPlus.Core;

public class DbHelper
{
    public SqliteConnection Connection { get; private set; }
    public bool KeepConnection { get; private set; }
    private string SqlCheckTableRecord = "SELECT name from sqlite_master WHERE name='record'";

    private string SqlCreateDb = """
                                 CREATE TABLE assets (
                                     id	INTEGER NOT NULL UNIQUE,
                                     data_b64	TEXT,
                                     md5 TEXT UNIQUE ,
                                     PRIMARY KEY("id" AUTOINCREMENT)
                                 );

                                 CREATE TABLE "record" (
                                     "id"	INTEGER NOT NULL UNIQUE,
                                     "hash_id"	TEXT UNIQUE,
                                     "data_md5"	TEXT,
                                     "text"	TEXT,
                                     "display_title"	TEXT,
                                     "senderapp"	TEXT,
                                     "icon_path"	TEXT,
                                     "icon_md5"	TEXT,
                                     "preview_image_path"	TEXT,
                                     "content_type"	INTEGER,
                                     "score"	INTEGER,
                                     "init_score"	INTEGER,
                                     "time"	TEXT,
                                     "create_time"	TEXT,
                                     "pined"	INTEGER,
                                     PRIMARY KEY("id" AUTOINCREMENT),
                                     FOREIGN KEY("icon_md5") REFERENCES "assets"("md5"),
                                     FOREIGN KEY("data_md5") REFERENCES "assets"("md5") ON DELETE CASCADE
                                 );
                                 """;

    public DbHelper(SqliteConnection connection)
    {
        Connection = connection;
    }

    public DbHelper(
        string dbPath,
        SqliteCacheMode cache = SqliteCacheMode.Shared,
        SqliteOpenMode mode = SqliteOpenMode.ReadWriteCreate,
        bool keepConnection = true
    )
    {
        var connString = new SqliteConnectionStringBuilder()
        {
            DataSource = dbPath,
            Mode = mode,
            ForeignKeys = true,
            Cache = cache,
        }.ToString();
        Connection = new SqliteConnection(connString);
        KeepConnection = keepConnection;
    }

    public void CreateDb()
    {
        Connection.Open();
        var name = Connection.QueryFirstOrDefault<string>(SqlCheckTableRecord);
        if (name == "record")
            return;
        var r = Connection.Execute(SqlCreateDb);
        CloseIfNotKeep();
    }

    public async void AddOneRecord(ClipboardData data)
    {
        Connection.Open();
        var sql = "INSERT OR IGNORE INTO assets(data_b64, md5) VALUES (@DataB64, @Md5);";
        // insert image data to table assets
        var iconB64 = data.Icon.ToBase64();
        var iconMd5 = iconB64.GetMd5();
        // insert data to table assets
        string dataB64 = data.DataToString();
        var dataMd5 = dataB64.GetMd5();
        var rows = await Connection.ExecuteAsync(
            sql,
            new List<Assets>
            {
                new() { DataB64 = iconB64, Md5 = iconMd5 },
                new() { DataB64 = dataB64, Md5 = dataMd5 },
            }
        );

        sql =
            @"INSERT OR IGNORE INTO record(
                    hash_id, data_md5, text, display_title, senderapp, 
                    icon_path, icon_md5, preview_image_path, content_type,
                    score, init_score, 'time', create_time, pined) 
                VALUES (
                    @HashId, @DataMd5, @Text, @DisplayTitle, @SenderApp, 
                    @IconPath, @IconMd5, @PreviewImagePath, @ContentType,
                    @Score, @InitScore, @Time, @CreateTime, @Pined);";
        var record = Record.FromClipboardData(data);

        var r = await Connection.ExecuteAsync(sql, record);
        CloseIfNotKeep();
    }

    public async void DeleteOneRecord(ClipboardData clipboardData)
    {
        var dataMd5 = clipboardData.DataToString().GetMd5();
        var sql = "SELECT COUNT() FROM record WHERE data_md5=@DataMd5;";
        var count = await Connection.QueryFirstAsync<int>(sql, new { DataMd5 = dataMd5 });
        // count > 1  means there are more than one record in table `record`
        // depends on corresponding record in table `assets`, in this condition,
        // we only delete record in table `record`
        sql = "DELETE FROM record WHERE hash_id=@HashId OR data_md5=@DataMd5;";
        if (count > 1)
            await Connection.ExecuteAsync(
                sql,
                new { HashId = clipboardData.HashId, DataMd5 = dataMd5 }
            );
        // otherwise, no record depends on assets, directly delete records
        // **both** in `record` and `assets` using foreign key constraint,
        // i.e., ON DELETE CASCADE
        else
        {
            sql = "PRAGMA foreign_keys = ON; DELETE FROM assets WHERE md5=@DataMd5;";
            await Connection.ExecuteAsync(sql, new { DataMd5 = dataMd5 });
        }

        CloseIfNotKeep();
    }

    public async void DeleteAllRecords()
    {
        var sql = "DROP TABLE IF EXISTS record; DROP TABLE IF EXISTS assets; VACUUM;";
        await Connection.ExecuteAsync(sql);
        CreateDb();
    }

    public async void PinOneRecord(ClipboardData data)
    {
        var sql = "INSERT OR IGNORE INTO assets(data_b64, md5) VALUES (@DataB64, @Md5);";
        var iconB64 = data.Icon.ToBase64();
        var iconMd5 = iconB64.GetMd5();
        var rows = await Connection.ExecuteAsync(
            sql,
            new List<Assets>
            {
                new() { DataB64 = iconB64, Md5 = iconMd5 },
            }
        );
        sql = "UPDATE record SET pined=@Pin, icon_md5=@IconMd5 WHERE hash_id=@HashId";
        await Connection.ExecuteAsync(
            sql,
            new
            {
                Pin = data.Pined,
                HashId = data.HashId,
                IconMd5 = iconMd5
            }
        );
        CloseIfNotKeep();
    }

    public async Task<LinkedList<ClipboardData>> GetAllRecord()
    {
        var sql = """
                  SELECT r.id as Id, a.data_b64 as DataMd5, r.text as Text, r.display_title as DisplayTitle,
                         r.senderapp as SenderApp, r.icon_path as IconPath, b.data_b64 as IconMd5,
                         r.preview_image_path as PreviewImagePath, r.content_type as ContentType,
                         r.score as Score, r.init_score as InitScore, r.time as Time,
                         r.create_time as CreateTime, r.pined as Pined, r.hash_id as HashId
                       FROM record r
                       LEFT JOIN assets a ON r.data_md5=a.md5
                       LEFT JOIN assets b ON r.icon_md5=b.md5;
                  """;
        var results = await Connection.QueryAsync<Record>(sql);
        LinkedList<ClipboardData> allRecord = new(results.Select(Record.ToClipboardData));
        CloseIfNotKeep();
        return allRecord;
    }

    public async void DeleteRecordByKeepTime(int contentType, int keepTime)
    {
        var sql = """
                  DELETE FROM record
                      WHERE strftime('%s', 'now') - strftime('%s', create_time) > @KeepTime*3600
                      AND content_type=@ContentType;
                  """;
        var r = await Connection.ExecuteAsync(
            sql,
            new { KeepTime = keepTime, ContentType = contentType }
        );
        CloseIfNotKeep();
    }

    public void Close()
    {
        if (Connection.State == ConnectionState.Open)
            Connection.Close();
    }

    public void Open()
    {
        if (Connection.State == ConnectionState.Closed)
            Connection.Open();
    }

    private async void CloseIfNotKeep()
    {
        if (!KeepConnection)
            await Connection.CloseAsync();
    }
}

public class Assets
{
    public int Id { get; set; }

    public string? DataB64 { get; set; }

    public string? Md5 { get; set; }
}

public class Record
{
    public int Id { get; set; }

    public string HashId { get; set; }
    public string DataMd5 { get; set; }
    public string Text { get; set; }
    public string DisplayTitle { get; set; }
    public string SenderApp { get; set; }
    public string IconPath { get; set; }
    public string IconMd5 { get; set; }
    public string PreviewImagePath { get; set; }
    public int ContentType { get; set; }
    public int Score { get; set; }
    public int InitScore { get; set; }
    public DateTime _time;

    public string Time
    {
        get => _time.ToString("O");
        set => _time = DateTime.Parse(value);
    }

    public DateTime _create_time;

    public string CreateTime
    {
        get => _create_time.ToString("O");
        set => _create_time = DateTime.Parse(value);
    }

    public bool Pined { get; set; }

    public static Record FromClipboardData(ClipboardData data)
    {
        var iconB64 = data.Icon.ToBase64();
        var iconMd5 = iconB64.GetMd5();
        // insert data to table assets
        string insertData = data.DataToString();
        var dataMd5 = insertData.GetMd5();
        var record = new Record
        {
            HashId = data.HashId,
            DataMd5 = dataMd5,
            Text = data.Text,
            DisplayTitle = data.DisplayTitle,
            SenderApp = data.SenderApp,
            IconPath = data.IconPath,
            IconMd5 = iconMd5,
            PreviewImagePath = data.PreviewImagePath,
            ContentType = (int)data.Type,
            Score = data.Score,
            InitScore = data.InitScore,
            Time = data.Time.ToString("O"),
            CreateTime = data.CreateTime.ToString("O"),
            Pined = data.Pined,
        };
        return record;
    }

    public static ClipboardData ToClipboardData(Record record)
    {
        var type = (CbContentType)record.ContentType;
        var clipboardData = new ClipboardData
        {
            HashId = record.HashId,
            Data = record.DataMd5,
            Text = record.Text,
            DisplayTitle = record.DisplayTitle,
            SenderApp = record.SenderApp,
            IconPath = record.IconPath,
            Icon = record.IconMd5.ToBitmapImage(),
            PreviewImagePath = record.PreviewImagePath,
            Type = type,
            Score = record.Score,
            InitScore = record.InitScore,
            Time = record._time,
            CreateTime = record._create_time,
            Pined = record.Pined,
        };
        switch (type)
        {
            case CbContentType.Text:
                break;
            case CbContentType.Image:
                clipboardData.Data = record.DataMd5.ToImage();
                break;
            case CbContentType.Files:
                clipboardData.Data = record.DataMd5.Split('\n');
                break;
            default:
                break;
        }

        return clipboardData;
    }
}
