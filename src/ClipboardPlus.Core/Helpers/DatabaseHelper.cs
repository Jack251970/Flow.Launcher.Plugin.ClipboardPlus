using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace ClipboardPlus.Core.Helpers;

public class DatabaseHelper : IDisposable
{
    #region Properties

    #region Connection

    public SqliteConnection Connection;
    public bool KeepConnection;

    #endregion

    #region Commands Strings

    private readonly string SqlSelectRecordTable = "SELECT name from sqlite_master WHERE name='record';";
    private readonly string SqlCreateDatabase =
        """
        CREATE TABLE assets (
            id	                    INTEGER NOT NULL UNIQUE,
            data_b64	            TEXT,
            data_md5                TEXT UNIQUE ,
            PRIMARY                 KEY("id" AUTOINCREMENT)
        );
        CREATE TABLE "record" (
            "id"	                INTEGER NOT NULL UNIQUE,
            "hash_id"	            TEXT UNIQUE,
            "data_md5"	            TEXT,
            "text"	                TEXT,
            "title"	                TEXT,
            "sender_app"	        TEXT,
            "preview_image_path"    TEXT,
            "data_type"	            INTEGER,
            "score"	                INTEGER,
            "init_score"	        INTEGER,
            "create_time"	        TEXT,
            "pinned"	            INTEGER,
            PRIMARY                 KEY("id" AUTOINCREMENT),
            FOREIGN                 KEY("data_md5") REFERENCES "assets"("data_md5") ON DELETE CASCADE
        );
        """;

    private readonly string SqlInsertAssets =
        "INSERT OR IGNORE INTO assets(data_b64, data_md5) VALUES (@DataB64, @DataMd5);";
    private readonly string SqlInsertRecord =
        @"INSERT OR IGNORE INTO record(
            hash_id, data_md5, text, title, sender_app, preview_image_path, 
            data_type, score, init_score, create_time, pinned) 
        VALUES (
            @HashId, @DataMd5, @Text, @Title, @SenderApp, @PreviewImagePath, 
            @DataType, @Score, @InitScore, @CreateTime, @Pinned);";

    private readonly string SqlSelectRecordCountByMd5 =
        "SELECT COUNT() FROM record WHERE data_md5=@DataMd5;";
    private readonly string SqlDeleteRecordAssets1 =
        "DELETE FROM record WHERE hash_id=@HashId OR data_md5=@DataMd5;";
    private readonly string SqlDeleteRecordAssets2 =
        "PRAGMA foreign_keys = ON; DELETE FROM assets WHERE data_md5=@DataMd5;";

    private readonly string SqlDeleteAllRecords =
        "DROP TABLE IF EXISTS record; DROP TABLE IF EXISTS assets; VACUUM;";

    private readonly string SqlUpdateRecordPinned =
        "UPDATE record SET pinned=@Pin WHERE hash_id=@HashId;";

    private readonly string SqlSelectAllRecord =
        """
        SELECT r.id as Id, a.data_b64 as DataMd5, r.text as Text, r.title as Title,
            r.sender_app as SenderApp, r.preview_image_path as PreviewImagePath,
            r.data_type as DataType, r.score as Score, r.init_score as InitScore,
            r.create_time as CreateTime, r.pinned as Pinned, r.hash_id as HashId
        FROM record r
        LEFT JOIN assets a ON r.data_md5=a.data_md5;
        """;

    private readonly string SqlDeleteRecordByKeepTime =
        """
        DELETE FROM record
        WHERE strftime('%s', 'now') - strftime('%s', create_time) > @KeepTime*3600
        AND data_type=@DataType;
        """;

    #endregion

    #endregion

    #region Constructors

    public DatabaseHelper(SqliteConnection connection)
    {
        Connection = connection;
    }

    public DatabaseHelper(
        string databasePath,
        SqliteCacheMode cache = SqliteCacheMode.Shared,
        SqliteOpenMode mode = SqliteOpenMode.ReadWriteCreate,
        bool keepConnection = true
    )
    {
        var connectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = databasePath,
            Mode = mode,
            ForeignKeys = true,
            Cache = cache,
        }.ToString();
        Connection = new SqliteConnection(connectionString);
        KeepConnection = keepConnection;
    }

    #endregion

    #region Methods

    #region Database Operations

    public async Task CreateDatabaseAsync()
    {
        Connection.Open();
        // check if `record` exists
        var name = Connection.QueryFirstOrDefault<string>(SqlSelectRecordTable);
        if (name != "record")
        {
            // if not exists, create `record` and `assets` table
            Connection.Execute(SqlCreateDatabase);
            await CloseIfNotKeepAsync();
        }
    }

    // TODO: Optimize large asset saving performance.
    public async Task AddOneRecordAsync(ClipboardData data)
    {
        Connection.Open();
        // insert data
        var dataB64 = data.DataToString();
        var dataMd5 = dataB64.GetMd5();
        var assets = new List<Asset>
        {
            new() { DataB64 = dataB64, DataMd5 = dataMd5 },
        };
        await Connection.ExecuteAsync(SqlInsertAssets, assets);
        // insert record
        // note: you must insert record after data
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
        {
            await Connection.ExecuteAsync(
                SqlDeleteRecordAssets1,
                new { clipboardData.HashId, DataMd5 = dataMd5 }
            );
        }
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
        await CreateDatabaseAsync();
    }

    public async Task PinOneRecordAsync(ClipboardData data)
    {
        // update record
        var record = new { Pin = data.Pinned, data.HashId };
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

    public async Task DeleteRecordByKeepTimeAsync(int dataType, int keepTime)
    {
        await Connection.ExecuteAsync(
            SqlDeleteRecordByKeepTime,
            new { KeepTime = keepTime, DataType = dataType }
        );
        await CloseIfNotKeepAsync();
    }

    #endregion

    #region Connection Management

    public async Task OpenAsync()
    {
        if (Connection.State == ConnectionState.Closed)
        {
            await Connection.OpenAsync();
        }
    }

    public void Open()
    {
        if (Connection.State == ConnectionState.Closed)
        {
            Connection.Open();
        }
    }

    public async Task CloseAsync()
    {
        if (Connection.State == ConnectionState.Open)
        {
            await Connection.CloseAsync();
        }
    }

    public void Close()
    {
        if (Connection.State == ConnectionState.Open)
        {
            Connection.Close();
        }
    }

    private async Task CloseIfNotKeepAsync()
    {
        if (!KeepConnection)
        {
            await Connection.CloseAsync();
        }
    }

    #endregion

    #region IDisposable Interface

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Connection.Dispose();
            Connection = null!;
        }
    }

    #endregion

    #endregion
}
