using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace ClipboardPlus.Core.Helpers;

public class DbHelpers : IDisposable
{
    public SqliteConnection Connection { get; init; }
    public bool KeepConnection { get; init; }

    #region Command Strings

    private readonly string SqlCheckTableRecord = "SELECT name from sqlite_master WHERE name='record';";
    private readonly string SqlCreateDatabase =
        """
        CREATE TABLE assets (
            id	        INTEGER NOT NULL UNIQUE,
            data_b64	TEXT,
            md5         TEXT UNIQUE ,
            PRIMARY KEY("id" AUTOINCREMENT)
        );
        CREATE TABLE "record" (
            "id"	                INTEGER NOT NULL UNIQUE,
            "hash_id"	            TEXT UNIQUE,
            "data_md5"	            TEXT,
            "text"	                TEXT,
            "display_title"	        TEXT,
            "senderapp"	            TEXT,
            "icon_path"	            TEXT,
            "icon_md5"	            TEXT,
            "preview_image_path"    TEXT,
            "content_type"	        INTEGER,
            "score"	                INTEGER,
            "init_score"	        INTEGER,
            "time"	                TEXT,
            "create_time"	        TEXT,
            "pinned"	            INTEGER,
            PRIMARY KEY("id" AUTOINCREMENT),
            FOREIGN KEY("icon_md5") REFERENCES "assets"("md5"),
            FOREIGN KEY("data_md5") REFERENCES "assets"("md5") ON DELETE CASCADE
        );
        """;

    private readonly string SqlInsertAssets =
        "INSERT OR IGNORE INTO assets(data_b64, md5) VALUES (@DataB64, @Md5);";
    private readonly string SqlInsertRecord =
        @"INSERT OR IGNORE INTO record(
            hash_id, data_md5, text, display_title, senderapp, 
            icon_path, icon_md5, preview_image_path, content_type,
            score, init_score, 'time', create_time, pinned) 
        VALUES (
            @HashId, @DataMd5, @Text, @DisplayTitle, @SenderApp, 
            @IconPath, @IconMd5, @PreviewImagePath, @ContentType,
            @Score, @InitScore, @Time, @CreateTime, @Pinned);";

    private readonly string SqlSelectRecordCountByMd5 =
        "SELECT COUNT() FROM record WHERE data_md5=@DataMd5;";
    private readonly string SqlDeleteRecordAssets1 =
        "DELETE FROM record WHERE hash_id=@HashId OR data_md5=@DataMd5;";
    private readonly string SqlDeleteRecordAssets2 =
        "PRAGMA foreign_keys = ON; DELETE FROM assets WHERE md5=@DataMd5;";

    private readonly string SqlDeleteAllRecords =
        "DROP TABLE IF EXISTS record; DROP TABLE IF EXISTS assets; VACUUM;";

    private readonly string SqlUpdateRecordPinned =
        "UPDATE record SET pinned=@Pin, icon_md5=@IconMd5 WHERE hash_id=@HashId;";

    private readonly string SqlSelectAllRecord =
        """
        SELECT r.id as Id, a.data_b64 as DataMd5, r.text as Text, r.display_title as DisplayTitle,
            r.senderapp as SenderApp, r.icon_path as IconPath, b.data_b64 as IconMd5,
            r.preview_image_path as PreviewImagePath, r.content_type as ContentType,
            r.score as Score, r.init_score as InitScore, r.time as Time,
            r.create_time as CreateTime, r.pinned as Pinned, r.hash_id as HashId
        FROM record r
        LEFT JOIN assets a ON r.data_md5=a.md5
        LEFT JOIN assets b ON r.icon_md5=b.md5;
        """;

    private readonly string SqlDeleteRecordByKeepTime =
        """
        DELETE FROM record
        WHERE strftime('%s', 'now') - strftime('%s', create_time) > @KeepTime*3600
        AND content_type=@ContentType;
        """;

    #endregion

    #region Constructors

    public DbHelpers(SqliteConnection connection)
    {
        Connection = connection;
    }

    public DbHelpers(
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

    #endregion

    #region Public Methods

    public async Task CreateDbAsync()
    {
        Connection.Open();
        // check if `record` exists
        var name = Connection.QueryFirstOrDefault<string>(SqlCheckTableRecord);
        // if not exists, create `record` and `assets` table
        if (name != "record")
        {
            Connection.Execute(SqlCreateDatabase);
            await CloseIfNotKeepAsync();
        }
    }

    // TODO: Optimize large assets saving performance.
    public async Task AddOneRecordAsync(ClipboardData data)
    {
        Connection.Open();
        // insert assets
        var iconB64 = data.Icon.ToBase64();
        var iconMd5 = iconB64.GetMd5();
        var dataB64 = data.DataToString();
        var dataMd5 = dataB64.GetMd5();
        var assets = new List<Assets>
        {
            new() { DataB64 = iconB64, Md5 = iconMd5 },
            new() { DataB64 = dataB64, Md5 = dataMd5 },
        };
        await Connection.ExecuteAsync(SqlInsertAssets, assets);
        // insert record
        // note: you must insert record after assets, because record depends on assets
        var record = Record.FromClipboardData(data);
        await Connection.ExecuteAsync(SqlInsertRecord, record);
        await CloseIfNotKeepAsync();
    }

    public async Task DeleteOneRecordAsync(ClipboardData clipboardData)
    {
        var dataMd5 = clipboardData.DataToString().GetMd5();
        var count = await Connection.QueryFirstAsync<int>(
            SqlSelectRecordCountByMd5, 
            new { DataMd5 = dataMd5 }
        );
        // count > 1  means there are more than one record in table `record`
        // depends on corresponding record in table `assets`, in this condition,
        // we only delete record in table `record`
        if (count > 1)
            await Connection.ExecuteAsync(
                SqlDeleteRecordAssets1,
                new { clipboardData.HashId, DataMd5 = dataMd5 }
            );
        // otherwise, no record depends on assets, directly delete records
        // both in `record` and `assets` using foreign key constraint,
        // i.e., ON DELETE CASCADE
        else
        {
            await Connection.ExecuteAsync(
                SqlDeleteRecordAssets2, 
                new { DataMd5 = dataMd5 }
            );
        }
        await CloseIfNotKeepAsync();
    }

    public async Task DeleteAllRecordsAsync()
    {
        await Connection.ExecuteAsync(SqlDeleteAllRecords);
        await CreateDbAsync();
    }

    public async Task PinOneRecordAsync(ClipboardData data)
    {
        // insert assets
        var iconB64 = data.Icon.ToBase64();
        var iconMd5 = iconB64.GetMd5();
        var assets = new List<Assets>
        {
            new() { DataB64 = iconB64, Md5 = iconMd5 },
        };
        await Connection.ExecuteAsync(SqlInsertAssets, assets);
        // update record
        var record = new { Pin = data.Pinned, data.HashId, IconMd5 = iconMd5 };
        await Connection.ExecuteAsync(SqlUpdateRecordPinned, record);
        await CloseIfNotKeepAsync();
    }

    public async Task<LinkedList<ClipboardData>> GetAllRecordAsync()
    {
        var results = await Connection.QueryAsync<Record>(SqlSelectAllRecord);
        LinkedList<ClipboardData> allRecord = new(results.Select(Record.ToClipboardData));
        await CloseIfNotKeepAsync();
        return allRecord;
    }

    public async Task DeleteRecordByKeepTimeAsync(int contentType, int keepTime)
    {
        await Connection.ExecuteAsync(
            SqlDeleteRecordByKeepTime,
            new { KeepTime = keepTime, ContentType = contentType }
        );
        await CloseIfNotKeepAsync();
    }

    public async Task OpenAsync()
    {
        if (Connection.State == ConnectionState.Closed)
        {
            await Connection.OpenAsync();
        }
    }

    public async Task CloseAsync()
    {
        if (Connection.State == ConnectionState.Open)
        {
            await Connection.CloseAsync();
        }
    }

    public void Dispose()
    {
        Connection.Dispose();
    }

    #endregion

    private async Task CloseIfNotKeepAsync()
    {
        if (!KeepConnection)
        {
            await Connection.CloseAsync();
        }
    }
}
