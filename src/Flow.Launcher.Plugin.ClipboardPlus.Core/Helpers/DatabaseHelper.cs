using Dapper;
using Microsoft.Data.Sqlite;
using System.Data;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

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
        CREATE TABLE asset (
            id	                    INTEGER NOT NULL UNIQUE,
            data_b64	            TEXT,
            unicode_text_b64        TEXT,
            hash_id                 TEXT UNIQUE,
            PRIMARY                 KEY("id" AUTOINCREMENT)
        );
        CREATE TABLE "record" (
            "id"	                INTEGER NOT NULL UNIQUE,
            "hash_id"	            TEXT UNIQUE,
            "data_md5_b64"	        TEXT,
            "sender_app"	        TEXT,
            "cached_image_path"     TEXT,
            "data_type"	            INTEGER,
            "init_score"	        INTEGER,
            "create_time"	        TEXT,
            "pinned"	            INTEGER,
            "encrypt_data"          INTEGER,
            "unicode_text"          TEXT,
            PRIMARY                 KEY("id" AUTOINCREMENT),
            FOREIGN                 KEY("hash_id") REFERENCES "asset"("hash_id") ON DELETE CASCADE
        );
        """;

    private readonly string SqlInsertAsset =
        @"INSERT OR IGNORE INTO asset(
            data_b64, unicode_text_b64, hash_id)
        VALUES (
            @DataB64, @UnicodeTextB64, @HashId);";
    private readonly string SqlInsertRecord =
        @"INSERT OR IGNORE INTO record(
            hash_id, data_md5_b64, sender_app, cached_image_path, 
            data_type, init_score, create_time, pinned, encrypt_data,
            unicode_text)
        VALUES (
            @HashId, @DataMd5B64, @SenderApp, @CachedImagePath, 
            @DataType, @InitScore, @CreateTime, @Pinned, @EncryptData,
            '');";

    private readonly string SqlSelectRecordCountByMd5 =
        "SELECT COUNT() FROM record WHERE hash_id=@HashId;";
    private readonly string SqlDeleteRecordAsset1 =
        "DELETE FROM record WHERE hash_id=@HashId;";
    private readonly string SqlDeleteRecordAsset2 =
        "PRAGMA foreign_keys = ON; DELETE FROM asset WHERE hash_id=@HashId;";

    private readonly string SqlDeleteAllRecords =
        "DROP TABLE IF EXISTS record; DROP TABLE IF EXISTS asset; VACUUM;";

    private readonly string SqlUpdateRecordPinned =
        "UPDATE record SET pinned=@Pin WHERE hash_id=@HashId;";

    private readonly string SqlSelectAllRecord =
        """
        SELECT r.id as Id, a.data_b64 as DataMd5B64, r.sender_app as SenderApp, 
            r.cached_image_path as CachedImagePath, r.data_type as DataType, 
            r.init_score as InitScore, r.encrypt_data as EncryptData,
            r.create_time as CreateTime, r.pinned as Pinned, r.hash_id as HashId,
            a.unicode_text_b64 as UnicodeText
        FROM record r
        LEFT JOIN asset a ON r.hash_id=a.hash_id;
        """;

    private readonly string SqlDeleteRecordByKeepTime =
        """
        DELETE FROM record
        WHERE strftime('%s', 'now') - strftime('%s', create_time) > @KeepTime*3600
        AND data_type=@DataType;
        """;

    private readonly string SqlDeleteUnpinnedRecords =
        "DELETE FROM record WHERE pinned=0;";

    #endregion

    #endregion

    #region Constructors

    public DatabaseHelper(SqliteConnection connection)
    {
        Connection = connection;
    }

    public DatabaseHelper(
        string databasePath,
        SqliteCacheMode cache = SqliteCacheMode.Default,
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

    public async Task InitializeDatabaseAsync()
    {
        await HandleOpenCloseAsync(async () =>
        {
            // check if `record` exists
            var name = Connection.QueryFirstOrDefault<string>(SqlSelectRecordTable);
            if (name != "record")
            {
                // if not exists, create `record` and `asset` table
                await Connection.ExecuteAsync(SqlCreateDatabase);
            }
        });
    }

#if DEBUG
    public async Task AddOneRecordAsync(ClipboardData data, Action<string>? action = null)
#else
    public async Task AddOneRecordAsync(ClipboardData data)
#endif
    {
        await HandleOpenCloseAsync(async () =>
        {
            // insert asset
            var assets = new List<Asset>
            {
                Asset.FromClipboardData(data)
            };
            await Connection.ExecuteAsync(SqlInsertAsset, assets);
            // insert record
            // note: you must insert record after data
            var record = Record.FromClipboardData(data);
#if DEBUG
            if (record.DataType == (int)DataType.Files && record.EncryptData == true)
            {
                action?.Invoke($"{data}\n{record}\n{assets[0]}");
            }
#endif
            await Connection.ExecuteAsync(SqlInsertRecord, record);
        });
    }

    public async Task DeleteOneRecordAsync(ClipboardData clipboardData)
    {
        await HandleOpenCloseAsync(async () =>
        {
            await DeleteOneRecordByClipboardData(clipboardData);
        });
    }

    public async Task DeleteAllRecordsAsync()
    {
        await HandleOpenCloseAsync(async () =>
        {
            await Connection.ExecuteAsync(SqlDeleteAllRecords);
            await Connection.ExecuteAsync(SqlCreateDatabase);
        });
    }

    public async Task PinOneRecordAsync(ClipboardData data)
    {
        await HandleOpenCloseAsync(async () =>
        {
            var record = new { Pin = data.Pinned, data.HashId };
            await Connection.ExecuteAsync(SqlUpdateRecordPinned, record);
            await CloseIfNotKeepAsync();
        });
    }

#if DEBUG
    public async Task<LinkedList<ClipboardData>> GetAllRecordsAsync(Action<string>? action = null)
#else
    public async Task<LinkedList<ClipboardData>> GetAllRecordsAsync()
#endif
    {
        return await HandleOpenCloseAsync(async () =>
        {
            // query all records
            var results = await Connection.QueryAsync<Record>(SqlSelectAllRecord);
#if DEBUG
            foreach (var record in results)
            {
                try
                {
                    var _ = ClipboardData.FromRecord(record);
                }
                catch (Exception)
                {
                    action?.Invoke($"Exception: {record}");
                }
            }
#endif
            LinkedList<ClipboardData> allRecord = new(results.Select(ClipboardData.FromRecord));
            return allRecord;
        });
    }

    public async Task DeleteRecordsByKeepTimeAsync(int dataType, int keepTime)
    {
        await HandleOpenCloseAsync(async () =>
        {
            await Connection.ExecuteAsync(
                SqlDeleteRecordByKeepTime,
                new { KeepTime = keepTime, DataType = dataType }
            );
        });
    }

    public async Task DeleteInvalidRecordsAsync()
    {
        await HandleOpenCloseAsync(async () =>
        {
            // query all records
            var results = await Connection.QueryAsync<Record>(SqlSelectAllRecord);
            LinkedList<ClipboardData> allRecord = new(results.Select(ClipboardData.FromRecord));
            // delete invalid records
            var invalidRecords = allRecord.Where(x => !x.IsValid);
            foreach (var record in invalidRecords)
            {
                await DeleteOneRecordByClipboardData(record);
            }
        });
    }

    public async Task DeleteUnpinnedRecordsAsync()
    {
        await HandleOpenCloseAsync(async () =>
        {
            await Connection.ExecuteAsync(SqlDeleteUnpinnedRecords);
        });
    }

    private async Task DeleteOneRecordByClipboardData(ClipboardData clipboardData)
    {
        var hashId = clipboardData.HashId;
        var count = await Connection.QueryFirstAsync<int>(
            SqlSelectRecordCountByMd5,
            new { HashId = hashId }
        );
        // count > 1 means there are more than one record in table `record`
        // depends on corresponding record in table `asset`, in this condition,
        // we only delete record in table `record`
        if (count > 1)
        {
            await Connection.ExecuteAsync(
                SqlDeleteRecordAsset1,
                new { clipboardData.HashId }
            );
        }
        // otherwise, no record depends on `asset`, directly delete records
        // both in `record` and `asset` using foreign key constraint,
        // i.e., ON DELETE CASCADE
        else
        {
            await Connection.ExecuteAsync(
                SqlDeleteRecordAsset2,
                new { HashId = hashId }
            );
        }
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

    #region Extension functions

    private async Task<T> HandleOpenCloseAsync<T>(Func<Task<T>> func)
    {
        await OpenAsync();
        var result = await func();
        await CloseIfNotKeepAsync();
        return result;
    }

    private async Task HandleOpenCloseAsync(Func<Task> func)
    {
        await OpenAsync();
        await func();
        await CloseIfNotKeepAsync();
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
