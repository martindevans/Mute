using System.Data.Common;
using System.Data.SQLite;
using System.Text.Json;
using System.Threading.Tasks;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.DiceLang.AST;

namespace Mute.Moe.Services.DiceLang.Macros;

public class DatabaseMacroStorage
    : IMacroStorage
{
    private readonly IDatabaseService _database;

    public DatabaseMacroStorage(IDatabaseService database)
    {
        _database = database;

        try
        {
            _database.Exec("CREATE TABLE IF NOT EXISTS `DiceMacros` (`JSON` TEXT NOT NULL, `Namespace` TEXT NOT NULL, `Name` TEXT NOT NULL)");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    public async Task<MacroDefinition?> Find(string? ns, string name)
    {
        return await FindAll(ns, name).SingleOrDefaultAsync();
    }

    public IAsyncEnumerable<MacroDefinition> FindAll(string? ns, string? name)
    {
        if (ns == null && name == null)
            return Array.Empty<MacroDefinition>().ToAsyncEnumerable();

        const string FindMacros = "SELECT * FROM DiceMacros WHERE (Name = @Name OR @Name is NULL) AND (Namespace = @Namespace OR @Namespace is NULL)";

        return new SqlAsyncResult<MacroDefinition>(_database, PrepareQuery, ParseMacroDefinition);

        DbCommand PrepareQuery(IDatabaseService db)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText = FindMacros;
            cmd.Parameters.Add(new SQLiteParameter("@Namespace", System.Data.DbType.String) { Value = ns });
            cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) { Value = name });
            return cmd;
        }
    }

    public async Task Create(MacroDefinition definition)
    {
        const string InsertMacroSql = "INSERT INTO `DiceMacros` (`JSON`, `Namespace`, `Name`) VALUES (@JSON, @Namespace, @Name);";

        var json = JsonSerializer.Serialize(new DbMacroJson(definition.ParameterNames, definition.Root));

        // Insert into the database
        await using var cmd = _database.CreateCommand();
        cmd.CommandText = InsertMacroSql;
        cmd.Parameters.Add(new SQLiteParameter("@JSON", System.Data.DbType.String) { Value = json });
        cmd.Parameters.Add(new SQLiteParameter("@Namespace", System.Data.DbType.String) { Value = definition.Namespace });
        cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) { Value = definition.Name });

        await cmd.ExecuteNonQueryAsync();
    }

    public async Task Delete(string ns, string name)
    {
        const string DeleteMacroSql = "DELETE FROM `DiceMacros` WHERE Namespace = @Namespace AND Name = @Name";


        await using var cmd = _database.CreateCommand();
        cmd.CommandText = DeleteMacroSql;
        cmd.Parameters.Add(new SQLiteParameter("@Namespace", System.Data.DbType.String) { Value = ns });
        cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) { Value = name });

        await cmd.ExecuteNonQueryAsync();
    }

    private static MacroDefinition ParseMacroDefinition(DbDataReader reader)
    {
        var deserialized = JsonSerializer.Deserialize<DbMacroJson>((string)reader["JSON"])!;

        return new MacroDefinition(
            (string)reader["Namespace"],
            (string)reader["Name"],
            deserialized.ParameterNames,
            deserialized.Root
        );
    }

    private record DbMacroJson(IReadOnlyList<string> ParameterNames, IAstNode Root);
}