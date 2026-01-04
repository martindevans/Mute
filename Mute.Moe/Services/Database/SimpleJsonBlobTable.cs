using System.Data.Common;
using System.Data.SQLite;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace Mute.Moe.Services.Database;

/// <summary>
/// A simple key/value store table that stores a JSON serialized blob by ulong id
/// </summary>
/// <typeparam name="TBlob"></typeparam>
public abstract class SimpleJsonBlobTable<TBlob>
    : IKeyValueStorage<TBlob>
    where TBlob : class
{
    private readonly string _putSql;
    private readonly string _getSql;
    private readonly string _deleteSql;
    private readonly string _clearSql;
    private readonly string _countSql;
    private readonly string _randomSql;

    private readonly string _tableName;
    private readonly IDatabaseService _database;

    /// <summary>
    /// Create a new table to store JSON blobs
    /// </summary>
    /// <param name="tableName"></param>
    /// <param name="database"></param>
    protected SimpleJsonBlobTable(string tableName, IDatabaseService database)
    {
        _tableName = tableName;
        _database = database;

        _putSql = $"INSERT OR REPLACE into {tableName} (ID, Json) values(@ID, @Json)";
        _getSql = $"SELECT Json FROM {tableName} WHERE ID = @ID";
        _deleteSql = $"DELETE FROM {tableName} WHERE ID = @ID";
        _clearSql = $"DELETE FROM {tableName}";
        _countSql = $"SELECT COUNT(*) FROM {tableName}";
        _randomSql = $"SELECT * FROM {tableName} ORDER BY RANDOM() LIMIT 1;";

        try
        {
            _database.Exec($"CREATE TABLE IF NOT EXISTS `{tableName}` (`ID` TEXT NOT NULL, `Json` TEXT NOT NULL)");
            _database.Exec($"CREATE INDEX IF NOT EXISTS `{tableName}ById` ON '{tableName}' (ID ASC);");
        }
        catch (Exception e)
        {
            Log.Error(e, "Creating SimpleJsonBlobTable '{0}' failed", tableName);
        }
    }

    /// <inheritdoc />
    public async Task Put(ulong id, TBlob data)
    {
        await Delete(id);

        var json = JsonSerializer.Serialize(data);
        try
        {
            await using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = _putSql;
                cmd.Parameters.Add(new SQLiteParameter("@ID", System.Data.DbType.String) { Value = id.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@Json", System.Data.DbType.String) { Value = json });
                await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "PUT into SimpleJsonBlobTable '{0}' failed. Key={1}.", _tableName, id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TBlob?> Get(ulong id)
    {
        try
        {
            await using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = _getSql;
                cmd.Parameters.Add(new SQLiteParameter("@ID", System.Data.DbType.String) { Value = id.ToString() });
                return await Read(cmd);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "GET from SimpleJsonBlobTable '{0}' failed. Key={1}.", _tableName, id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> Delete(ulong id)
    {
        try
        {
            await using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = _deleteSql;
                cmd.Parameters.Add(new SQLiteParameter("@ID", System.Data.DbType.String) { Value = id.ToString() });
                var count = await cmd.ExecuteNonQueryAsync();
                return count > 0;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "DELETE from SimpleJsonBlobTable '{0}' failed. Key={1}.", _tableName, id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task Clear()
    {
        try
        {
            await using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = _clearSql;
                var count = await cmd.ExecuteNonQueryAsync();
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "CLEAR SimpleJsonBlobTable '{0}' failed.", _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<int> Count()
    {
        try
        {
            await using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = _countSql;
                var count = (int?)await cmd.ExecuteScalarAsync();
                return count ?? 0;
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "COUNT SimpleJsonBlobTable '{0}' failed.", _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<TBlob?> Random()
    {
        try
        {
            await using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = _randomSql;
                return await Read(cmd);
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "RANDOM from SimpleJsonBlobTable '{0}' failed.", _tableName);
            throw;
        }
    }

    /// <summary>
    /// Read a TBlob from the given <see cref="DbCommand"/>
    /// </summary>
    /// <param name="cmd"></param>
    /// <returns></returns>
    protected virtual async Task<TBlob?> Read(DbCommand cmd)
    {
        var json = (string?)await cmd.ExecuteScalarAsync();

        if (json == null)
            return null;
        return JsonSerializer.Deserialize<TBlob>(json);
    }
}