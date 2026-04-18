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
            using (var connection = _database.GetConnection())
            {
                connection.Execute($"CREATE TABLE IF NOT EXISTS `{tableName}` (`ID` TEXT NOT NULL, `Json` TEXT NOT NULL)");
                connection.Execute($"CREATE INDEX IF NOT EXISTS `{tableName}ById` ON '{tableName}' (ID ASC);");
            }
        }
        catch (Exception e)
        {
            Log.Error(e, "Creating SimpleJsonBlobTable '{0}' failed", tableName);
        }
    }

    /// <inheritdoc />
    public async Task Put(ulong id, TBlob data)
    {
        using var connection = _database.GetConnection();
        
        using (var tsx = connection.BeginTransaction())
        {
            await Delete(id, tsx);

            await connection.ExecuteAsync(
                _putSql,
                new
                {
                    ID = id.ToString(),
                    Json = JsonSerializer.Serialize(data)
                },
                transaction: tsx
            );

            tsx.Commit();
        }
    }

    /// <inheritdoc />
    public async Task<TBlob?> Get(ulong id)
    {
        using var connection = _database.GetConnection();
        
        var json = await connection.QuerySingleOrDefaultAsync<string>(
            _getSql,
            new
            {
                ID = id.ToString()
            }
        );

        return await Read(json);
    }

    /// <inheritdoc />
    public Task<bool> Delete(ulong id)
    {
        using var connection = _database.GetConnection();

        using (var tsx = connection.BeginTransaction())
        {
            var result = Delete(id, tsx);
            tsx.Commit();
            return result;
        }
    }

    private async Task<bool> Delete(ulong id, IDbTransaction tsx)
    {
        var count = await tsx.Connection!.ExecuteAsync(
            _deleteSql,
            new
            {
                ID = id.ToString(),
            },
            transaction: tsx
        );

        return count > 0;
    }

    /// <inheritdoc />
    public async Task Clear()
    {
        using var connection = _database.GetConnection();
        await connection.ExecuteAsync(_clearSql);
    }

    /// <inheritdoc />
    public async Task<long> Count()
    {
        using var connection = _database.GetConnection();
        return await connection.ExecuteScalarAsync<long>(_countSql);
    }

    /// <inheritdoc />
    public async Task<TBlob?> Random()
    {
        using var connection = _database.GetConnection();
        var json = await connection.QuerySingleOrDefaultAsync<string>(_randomSql);
        return await Read(json);
    }

    /// <summary>
    /// Convert JSON into TBlob object
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