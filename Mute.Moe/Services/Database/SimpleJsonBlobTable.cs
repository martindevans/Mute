using System.Data.SQLite;
using System.Text.Json;
using System.Threading.Tasks;

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
    private readonly string _countSql;

    private readonly IDatabaseService _database;

    protected SimpleJsonBlobTable(string tableName, IDatabaseService database)
    {
        _database = database;
        _putSql = $"INSERT OR REPLACE into {tableName} (ID, Json) values(@ID, @Json)";
        _getSql = $"SELECT Json FROM {tableName} WHERE ID = @ID";
        _deleteSql = $"DELETE Json FROM {tableName} WHERE ID = @ID";
        _countSql = $"SELECT COUNT(*) FROM {tableName}";


        try
        {
            _database.Exec($"CREATE TABLE IF NOT EXISTS `{tableName}` (`ID` TEXT NOT NULL, `Json` TEXT NOT NULL)");
            _database.Exec($"CREATE INDEX IF NOT EXISTS `{tableName}ById` ON '{tableName}' (ID ASC);");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task Put(ulong id, TBlob data)
    {
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
            Console.WriteLine(e);
            throw;
        }
    }

    public async Task<TBlob?> Get(ulong id)
    {
        try
        {
            await using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = _getSql;
                cmd.Parameters.Add(new SQLiteParameter("@ID", System.Data.DbType.String) { Value = id.ToString() });
                var json = (string?)await cmd.ExecuteScalarAsync();

                if (json == null)
                    return null;
                return JsonSerializer.Deserialize<TBlob>(json);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

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
            Console.WriteLine(e);
            throw;
        }
    }

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
            Console.WriteLine(e);
            throw;
        }
    }
}