using Dapper;
using Microsoft.Data.Sqlite;
using System.Collections.Concurrent;
using System.Data;

namespace Flow.Launcher.Plugin.ClipboardPlus.Core.Helpers;

public class SqliteDatabase : IAsyncDisposable
{
    #region Properties

    #region Version

    private const string DatabaseVersion = "1.1";

    #region History

    private const string DatabaseVersion10 = "1.0";

    #endregion

    #endregion

    #region Score

    public int CurrentScore = 1;

    private readonly int ScoreInterval;

    #endregion

    #region Connection

    public SqliteConnection Connection;

    #endregion

    #region Context

    private readonly PluginInitContext? Context;

    #endregion

    #region Class Name

    private string ClassName => GetType().Name;

    #endregion

    #region Commands Strings

    private readonly string SqlSelectMetaTable = "SELECT name from sqlite_master WHERE name='meta';";
    private readonly string SqlCreateMeta =
        """
        CREATE TABLE "meta" (
            "key"	                TEXT NOT NULL UNIQUE,
            "value"	                TEXT,
            PRIMARY                 KEY("key")
        );
        """;

    private readonly string SqlSelectVersion = "SELECT value FROM meta WHERE key = 'DatabaseVersion';";
    private readonly string SqlUpdateVersion = "UPDATE meta SET value = @Version WHERE key = 'DatabaseVersion';";
    private readonly string SqlInsertVersion = "INSERT INTO meta (key, value) VALUES ('DatabaseVersion', @Version);";

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
            "create_time"	        TEXT,
            "datetime_score"        INTEGER,
            "pinned"	            INTEGER,
            "encrypt_data"          INTEGER,
            "unicode_text"          TEXT,
            "encrypt_key_md5"       TEXT,
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
            data_type, create_time, datetime_score, pinned, encrypt_data,
            unicode_text, encrypt_key_md5)
        VALUES (
            @HashId, @DataMd5B64, @SenderApp, @CachedImagePath, 
            @DataType, @CreateTime, @DatetimeScore, @Pinned, @EncryptData,
            @UnicodeText, @EncryptKeyMd5);";

    private readonly string SqlSelectRecordCountByMd5 =
        "SELECT COUNT() FROM record WHERE hash_id=@HashId;";
    private readonly string SqlDeleteRecordAsset1 =
        "DELETE FROM record WHERE hash_id=@HashId;";
    private readonly string SqlDeleteRecordAsset2 =
        "PRAGMA foreign_keys = ON; DELETE FROM asset WHERE hash_id=@HashId;";

    private readonly string SqlDeleteAllRecords =
        "DELETE FROM record; DELETE FROM asset;";

    private readonly string SqlUpdateRecordPinned =
        "UPDATE record SET pinned=@Pin WHERE hash_id=@HashId;";

    private readonly string SqlSelectAllRecord =
        """
        SELECT r.id as Id, a.data_b64 as DataMd5B64, r.sender_app as SenderApp, 
            r.cached_image_path as CachedImagePath, r.data_type as DataType, 
            r.encrypt_data as EncryptData, r.create_time as CreateTime, 
            r.datetime_score as DatetimeScore, r.pinned as Pinned, 
            r.hash_id as HashId, a.unicode_text_b64 as UnicodeText,
            r.encrypt_key_md5 as EncryptKeyMd5
        FROM record r
        LEFT JOIN asset a ON r.hash_id=a.hash_id;
        """;

    private readonly string SqlSelectAllRecordOrederedByDateTimeScore =
        """
        SELECT r.id as Id, a.data_b64 as DataMd5B64, r.sender_app as SenderApp, 
            r.cached_image_path as CachedImagePath, r.data_type as DataType, 
            r.encrypt_data as EncryptData, r.create_time as CreateTime, 
            r.datetime_score as DatetimeScore, r.pinned as Pinned, 
            r.hash_id as HashId, a.unicode_text_b64 as UnicodeText,
            r.encrypt_key_md5 as EncryptKeyMd5
        FROM record r
        LEFT JOIN asset a ON r.hash_id=a.hash_id
        ORDER BY r.datetime_score ASC;
        """;

    private readonly string SqlSelectLocalRecord =
        $"""
        SELECT r.id as Id, a.data_b64 as DataMd5B64, r.sender_app as SenderApp, 
            r.cached_image_path as CachedImagePath, r.data_type as DataType, 
            r.encrypt_data as EncryptData, r.create_time as CreateTime, 
            r.datetime_score as DatetimeScore, r.pinned as Pinned, 
            r.hash_id as HashId, a.unicode_text_b64 as UnicodeText,
            r.encrypt_key_md5 as EncryptKeyMd5
        FROM record r
        LEFT JOIN asset a ON r.hash_id=a.hash_id
        WHERE r.encrypt_key_md5='{StringUtils.EncryptKeyMd5}';
        """;

    private readonly string SqlSelectRecordByKeepTimeDataType =
        "SELECT * FROM record WHERE strftime('%s', 'now') - strftime('%s', create_time) > @KeepTime*3600 AND data_type=@DataType;";

    private readonly string SqlDeleteRecordByKeepTimeDataType =
        "DELETE FROM record WHERE strftime('%s', 'now') - strftime('%s', create_time) > @KeepTime*3600 AND data_type=@DataType;";

    private readonly string SqlSelectUnpinnedRecord =
        "SELECT * FROM record WHERE pinned=0;";

    private readonly string SqlDeleteUnpinnedRecords =
        "DELETE FROM record WHERE pinned=0;";

    private readonly string SqlSelectRecordsByEncryptKeyMd5 =
        "SELECT * FROM record WHERE encrypt_key_md5=@EncryptKeyMd5;";

    private readonly string SqlDeleteRecordsByEncryptKeyMd5 =
        "DELETE FROM record WHERE encrypt_key_md5=@EncryptKeyMd5;";

    #endregion

    #region Queued Tasks

    private readonly ConcurrentQueue<Func<Task>> _taskQueue = new();
    private readonly SemaphoreSlim _queueSemaphore = new(1, 1);  // Semaphore to synchronize task queue processing
    private bool _isProcessingQueue;

    #endregion

    #endregion

    #region Constructors

    public SqliteDatabase(
        SqliteConnection connection,
        int scoreInterval,
        PluginInitContext? context = null)
    {
        Connection = connection;
        ScoreInterval = scoreInterval;
        Context = context;
    }

    public SqliteDatabase(
        string databasePath,
        int scoreInterval,
        SqliteCacheMode cache = SqliteCacheMode.Default,
        SqliteOpenMode mode = SqliteOpenMode.ReadWriteCreate,
        PluginInitContext? context = null
    )
    {
        ScoreInterval = scoreInterval;
        var connectionString = new SqliteConnectionStringBuilder()
        {
            DataSource = databasePath,
            Mode = mode,
            ForeignKeys = true,
            Cache = cache,
        }.ToString();
        Connection = new SqliteConnection(connectionString);
        Context = context;
    }

    #endregion

    #region Methods

    #region Database Operations

    #region Version Check & Handle

    private string GetDatabaseVersionAsync()
    {
        var version = Connection.QueryFirstOrDefault<string>(SqlSelectVersion);
        return version ?? "0.0";
    }

    private async Task SetDatabaseVersionAsync(string version)
    {
        var existingVersion = Connection.QueryFirstOrDefault<string>(SqlSelectVersion);
        if (existingVersion != null)
        {
            await Connection.ExecuteAsync(SqlUpdateVersion, new { Version = version });
        }
        else
        {
            await Connection.ExecuteAsync(SqlInsertVersion, new { Version = version });
        }
    }

    #region Version 0.0

    private readonly string SqlDropTables =
        """
        DROP TABLE IF EXISTS record;
        DROP TABLE IF EXISTS asset;
        """;

    private readonly string SqlDeleteInitScoreAddDateTimeScoreEncryptKeyMd5Column =
        """
        ALTER TABLE record
        DROP COLUMN init_score;
        ALTER TABLE record
        ADD COLUMN datetime_score INTEGER DEFAULT 0;
        ALTER TABLE record
        ADD COLUMN encrypt_key_md5 TEXT DEFAULT '';
        """;

    private readonly string SqlUpdateRecordDatetimeScoreEncryptKeyMd5 =
        "UPDATE record SET datetime_score=@DatetimeScore, encrypt_key_md5=@EncryptKeyMd5 WHERE hash_id=@HashId;";

    #endregion

    private async Task UpdateDatabase(string currentVersion)
    {
        try
        {
            if (currentVersion == DatabaseVersion)
            {
                return;
            }

            if (currentVersion == DatabaseVersion10)
            {
                // 1.0
                // Delete init_score column in `record` table
                // Add datetime_score & encrypt_key_md5 columns in `record` table
                await Connection.ExecuteAsync(SqlDeleteInitScoreAddDateTimeScoreEncryptKeyMd5Column);
                var records = await Connection.QueryAsync<Record>(SqlSelectAllRecord);
                foreach (var record in records)
                {
                    record.DatetimeScore = Record.GetDateTimeScore(record.createTime);
                    record.EncryptKeyMd5 = StringUtils.EncryptKeyMd5;
                    await Connection.ExecuteAsync(SqlUpdateRecordDatetimeScoreEncryptKeyMd5, record);
                }
            }
            else
            {
                // 0.0
                // Drop existing `record` and `asset` tables
                await Connection.ExecuteAsync(SqlDropTables);
                // Recreate the tables with the updated schema
                await Connection.ExecuteAsync(SqlCreateDatabase);
            }
            await SetDatabaseVersionAsync(DatabaseVersion);
        }
        catch (Exception e)
        {
            Context?.API.LogException(ClassName, $"Update database to version {currentVersion} error!", e);
        }
    }

    #endregion

    public async Task InitializeDatabaseAsync()
    {
        await OpenAsync();

        // check if `meta` exists
        var name = Connection.QueryFirstOrDefault<string>(SqlSelectMetaTable);
        if (name != "meta")
        {
            // if not exists, create `meta` table
            await Connection.ExecuteAsync(SqlCreateMeta);
            await SetDatabaseVersionAsync("0.0");
        }
        // check if `record` exists
        name = Connection.QueryFirstOrDefault<string>(SqlSelectRecordTable);
        if (name != "record")
        {
            // if not exists, create `record` and `asset` table
            await Connection.ExecuteAsync(SqlCreateDatabase);
        }
        // update version
        var currentVersion = GetDatabaseVersionAsync();
        if (currentVersion != DatabaseVersion)
        {
            await UpdateDatabase(currentVersion);
        }
    }

#if DEBUG
    public async Task AddOneRecordAsync(ClipboardData data, bool needEncryptData, Action<string>? action = null)
#else
    public async Task AddOneRecordAsync(ClipboardData data, bool needEncryptData)
#endif
    {
        await ProcessTaskQueueAsync(async () =>
        {
            // insert asset
            var assets = new List<Asset>
            {
                Asset.FromClipboardData(data, needEncryptData)
            };
            await Connection.ExecuteAsync(SqlInsertAsset, assets);

            // insert record
            // note: you must insert record after data
            var record = Record.FromClipboardData(data, needEncryptData);
#if DEBUG
            if (record.DataType == (int)DataType.Files && record.EncryptData == true)
            {
                action?.Invoke($"{data}\n{record}\n{assets[0]}");
            }
#endif
            await Connection.ExecuteAsync(SqlInsertRecord, record);
        });
    }

    public async Task AddRecordsAsync(IEnumerable<ClipboardData> datas, bool needEncryptData)
    {
        if (!datas.Any())
        {
            return;
        }

        await ProcessTaskQueueAsync(async () =>
        {
            foreach (var data in datas)
            {
                // insert asset
                var assets = new List<Asset>
                {
                    Asset.FromClipboardData(data, needEncryptData)
                };
                await Connection.ExecuteAsync(SqlInsertAsset, assets);

                // insert record
                // note: you must insert record after data
                var record = Record.FromClipboardData(data, needEncryptData);
                await Connection.ExecuteAsync(SqlInsertRecord, record);
            }
        });
    }

    public async Task DeleteOneRecordAsync(ClipboardData data)
    {
        await ProcessTaskQueueAsync(async () =>
        {
            // delete one record
            await DeleteOneRecordByClipboardData(data.HashId);
        });
    }

    public async Task DeleteRecordsAsync(IEnumerable<string> hashIds)
    {
        if (!hashIds.Any())
        {
            return;
        }

        await ProcessTaskQueueAsync(async () =>
        {
            foreach (var data in hashIds)
            {
                // delete one record
                await DeleteOneRecordByClipboardData(data);
            }
        });
    }

    public async Task DeleteAllRecordsAsync()
    {
        await ProcessTaskQueueAsync(async () =>
        {
            // delete all records
            await Connection.ExecuteAsync(SqlDeleteAllRecords);
        });
    }

    public async Task PinOneRecordAsync(ClipboardData data)
    {
        await ProcessTaskQueueAsync(async () =>
        {
            // update record pinned status
            var record = new { Pin = data.Pinned, data.HashId };
            await Connection.ExecuteAsync(SqlUpdateRecordPinned, record);
        });
    }

    public async Task PinRecordsAsync(IEnumerable<ClipboardData> datas)
    {
        if (!datas.Any())
        {
            return;
        }

        await ProcessTaskQueueAsync(async () =>
        {
            // update record pinned status
            foreach (var data in datas)
            {
                var record = new { Pin = data.Pinned, data.HashId };
                await Connection.ExecuteAsync(SqlUpdateRecordPinned, record);
            }
        });
    }

#if DEBUG
    public async Task<List<ClipboardData>> GetAllRecordsAsync(bool needSort, Action<string>? action = null)
#else
    public async Task<List<ClipboardData>> GetAllRecordsAsync(bool needSort)
#endif
    {
        // query all records & return
        if (!needSort)
        {
            var results = await Connection.QueryAsync<Record>(SqlSelectAllRecord);
            return results.Select(ClipboardData.FromRecord).ToList();
        }

        // query all records & build record list
        var sortedResults = await Connection.QueryAsync<Record>(SqlSelectAllRecordOrederedByDateTimeScore);
        var allRecord = new List<ClipboardData>();
        foreach (var record in sortedResults)
        {
            try
            {
                var data = ClipboardData.FromRecord(record, CurrentScore);
                allRecord.Add(data);
                CurrentScore += ScoreInterval;
            }
            catch (Exception)
            {
#if DEBUG
                action?.Invoke($"Exception: {record}");
#endif
            }
        }
        return allRecord;
    }

    public async Task<List<ClipboardData>> GetLocalRecordsAsync()
    {
        var results = await Connection.QueryAsync<Record>(SqlSelectLocalRecord);
        return results.Select(ClipboardData.FromRecord).ToList();
    }

    public async Task DeleteRecordsByKeepTimeAsync(int dataType, int keepTime)
    {
        await ProcessTaskQueueAsync(async () =>
        {
            // query all records by keep time and data type
            var results = await Connection.QueryAsync<Record>(
                SqlSelectRecordByKeepTimeDataType,
                new { KeepTime = keepTime, DataType = dataType }
            );

            // delete records by keep time and data type
            await Connection.ExecuteAsync(
                SqlDeleteRecordByKeepTimeDataType,
                new { KeepTime = keepTime, DataType = dataType }
            );
        });
    }

    public async Task DeleteInvalidRecordsAsync()
    {
        await ProcessTaskQueueAsync(async () =>
        {
            // query all records
            var results = await Connection.QueryAsync<Record>(SqlSelectAllRecord);
            List<ClipboardData> allRecords = new(results.Select(ClipboardData.FromRecord));

            // delete invalid records
            var invalidRecords = allRecords.Where(x => !x.IsValid);
            foreach (var record in invalidRecords)
            {
                await DeleteOneRecordByClipboardData(record.HashId);
            }
        });
    }

    public async Task DeleteUnpinnedRecordsAsync()
    {
        await ProcessTaskQueueAsync(async () =>
        {
            // get upinned records
            var results = await Connection.QueryAsync<Record>(SqlSelectUnpinnedRecord);

            // delete unpinned records
            await Connection.ExecuteAsync(SqlDeleteUnpinnedRecords);
        });
    }

    public async Task DeleteRecordsByEncryptKeyMd5(string encryptKeyMd5)
    {
        await ProcessTaskQueueAsync(async () =>
        {
            // query records by encrypt key md5
           var results = await Connection.QueryAsync<Record>(
               SqlSelectRecordsByEncryptKeyMd5, 
               new { EncryptKeyMd5 = encryptKeyMd5 }
            );

            // delete records by encrypt key md5
            await Connection.ExecuteAsync(
                SqlDeleteRecordsByEncryptKeyMd5,
                new { EncryptKeyMd5 = encryptKeyMd5 }
            );
        });
    }

    private async Task DeleteOneRecordByClipboardData(string hashId)
    {
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
                new { HashId = hashId }
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

    public async Task CloseAsync()
    {
        if (Connection.State == ConnectionState.Open)
        {
            await Connection.CloseAsync();
        }
    }

    #endregion

    #region Extension functions

    private async Task WaitTaskQueueAsync()
    {
        if (!_isProcessingQueue)
        {
            return;
        }

        // wait for the current task queue to finish
        await _queueSemaphore.WaitAsync();
        _queueSemaphore.Release();
    }

    private async Task ProcessTaskQueueAsync(Func<Task> func)
    {
        _taskQueue.Enqueue(func);

        if (_isProcessingQueue)
        {
            return;
        }

        _isProcessingQueue = true;
        await _queueSemaphore.WaitAsync(); // Ensure only one task queue process at a time

        try
        {
            while (_taskQueue.TryDequeue(out var task))
            {
                await task();
            }
        }
        finally
        {
            _isProcessingQueue = false;
            _queueSemaphore.Release();
        }
    }

    #endregion

    #endregion

    #region IDisposable Interface

    private bool _disposed;

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        await DisposeAsync(true);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsync(bool disposing)
    {
        if (disposing)
        {
            await WaitTaskQueueAsync();
            await Connection.CloseAsync();
            await Connection.DisposeAsync();
            Connection = null!;
            _disposed = true;
        }
    }

    #endregion
}
