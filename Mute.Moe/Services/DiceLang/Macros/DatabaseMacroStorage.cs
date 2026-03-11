using System.Data;
using System.Text.Json;
using System.Threading.Tasks;
using Dapper;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.DiceLang.AST;
using Serilog;

namespace Mute.Moe.Services.DiceLang.Macros;

/// <inheritdoc />
public class DatabaseMacroStorage
    : IMacroStorage
{
    private readonly IDatabaseService _database;

    /// <summary>
    /// Create a new <see cref="DatabaseMacroStorage"/>
    /// </summary>
    /// <param name="database"></param>
    public DatabaseMacroStorage(IDatabaseService database)
    {
        _database = database;

        try
        {
            _database.Exec("CREATE TABLE IF NOT EXISTS `DiceMacros` (`JSON` TEXT NOT NULL, `Namespace` TEXT NOT NULL, `Name` TEXT NOT NULL)");
        }
        catch (Exception e)
        {
            Log.Error(e, "Creating 'DiceMacros' table failed");
        }
    }

    /// <inheritdoc />
    public async Task<MacroDefinition?> Find(string? ns, string name)
    {
        return await FindAll(ns, name).SingleOrDefaultAsync();
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<MacroDefinition> FindAll(string? ns, string? name)
    {
        const string FindMacros = "SELECT * FROM DiceMacros WHERE (Name = @Name OR @Name is NULL) AND (Namespace = @Namespace OR @Namespace is NULL)";

        if (ns == null && name == null)
            yield break;

        using var reader = await _database.Connection.ExecuteReaderAsync(
            FindMacros,
            new
            {
                Namespace = ns,
                Name = name
            }
        );

        while (reader.Read())
            yield return ParseMacroDefinition(reader);
    }

    /// <inheritdoc />
    public async Task Create(MacroDefinition definition)
    {
        const string InsertMacroSql = "INSERT INTO `DiceMacros` (`JSON`, `Namespace`, `Name`) VALUES (@JSON, @Namespace, @Name);";

        var json = JsonSerializer.Serialize(new DbMacroJson(definition.ParameterNames, definition.Root));

        await _database.Connection.ExecuteAsync(
            InsertMacroSql,
            new
            {
                JSON = json,
                Namespace = definition.Namespace,
                Name = definition.Name
            }
        );
    }

    /// <inheritdoc />
    public async Task<bool> Delete(string ns, string name)
    {
        const string DeleteMacroSql = "DELETE FROM `DiceMacros` WHERE Namespace = @Namespace AND Name = @Name";

        return await _database.Connection.ExecuteAsync(
            DeleteMacroSql,
            new
            {
                Namespace = ns,
                Name = name
            }
        ) > 0;
    }

    private static MacroDefinition ParseMacroDefinition(IDataReader reader)
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