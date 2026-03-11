using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
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

        _putSql = $"INSERT into {tableName} (ID, Json) values(@ID, @Json)";
        _getSql = $"SELECT Json FROM {tableName} WHERE ID = @ID";
        _deleteSql = $"DELETE FROM {tableName} WHERE ID = @ID";
        _clearSql = $"DELETE FROM {tableName}";
        _countSql = $"SELECT COUNT(*) FROM {tableName}";
        _randomSql = $"SELECT Json FROM {tableName} ORDER BY RANDOM() LIMIT 1;";

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
        using (var tsx = _database.Connection.BeginTransaction())
        {
            await Delete(id, tsx);

            try
            {
                await _database.Connection.ExecuteAsync(
                    _putSql,
                    new
                    {
                        ID = id.ToString(),
                        Json = JsonSerializer.Serialize(data)
                    }
                );

                tsx.Commit();
            }
            catch (Exception e)
            {
                Log.Error(e, "PUT into SimpleJsonBlobTable '{0}' failed. Key={1}.", _tableName, id);
                throw;
            }
        }
    }

    /// <inheritdoc />
    public async Task<TBlob?> Get(ulong id)
    {
        try
        {
            var json = await _database.Connection.QuerySingleOrDefaultAsync<string>(
                _getSql,
                new
                {
                    ID = id.ToString()
                }
            );

            return await Read(json);
        }
        catch (Exception e)
        {
            Log.Error(e, "GET from SimpleJsonBlobTable '{0}' failed. Key={1}.", _tableName, id);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> Delete(ulong id)
    {
        return Delete(id, null);
    }

    private async Task<bool> Delete(ulong id, IDbTransaction? tsx)
    {
        try
        {
            var count = await _database.Connection.ExecuteAsync(
                _deleteSql,
                new
                {
                    ID = id.ToString(),
                },
                transaction: tsx
            );

            return count > 0;
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
            await _database.Connection.ExecuteAsync(_clearSql);
        }
        catch (Exception e)
        {
            Log.Error(e, "CLEAR SimpleJsonBlobTable '{0}' failed.", _tableName);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<long> Count()
    {
        try
        {
            return await _database.Connection.ExecuteScalarAsync<long>(_countSql);
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
            var json = await _database.Connection.QuerySingleOrDefaultAsync<string>(_randomSql);
            return await Read(json);
        }
        catch (Exception e)
        {
            Log.Error(e, "RANDOM from SimpleJsonBlobTable '{0}' failed.", _tableName);
            throw;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>
    protected virtual async Task<TBlob?> Read(string? json)
    {
        if (json == null)
            return null;
        return JsonSerializer.Deserialize<TBlob>(json);
    }
}