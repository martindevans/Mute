using System.Data;
using Discord;
using Mute.Moe.Services.Database;
using Serilog;
using System.Threading.Tasks;
using Dapper;

namespace Mute.Moe.Services.Groups;

/// <inheritdoc />
public class DatabaseGroupService
    : IGroups
{
    #region SQL
    private const string FindUnlockedRoleByCompositeId = "SELECT 1 FROM UnlockedRoles WHERE RoleId = @RoleId AND GuildId = @GuildId";
    private const string FindUnlockedRoleByGuildId = "SELECT * FROM UnlockedRoles WHERE GuildId = @GuildId";

    private const string InsertUnlockSql = "INSERT OR IGNORE into UnlockedRoles (RoleId, GuildId) values(@RoleId, @GuildId)";
    private const string DeleteUnlockSql = "Delete from UnlockedRoles Where RoleId = @RoleId AND GuildId = @GuildId";
    #endregion

    private readonly IDatabaseService _database;

    /// <summary>
    /// Create a new <see cref="DatabaseGroupService"/>
    /// </summary>
    /// <param name="database"></param>
    public DatabaseGroupService(IDatabaseService database)
    {
        _database = database;

        // Create database structure
        try
        {
            _database.Exec("CREATE TABLE IF NOT EXISTS `UnlockedRoles` (`RoleId` TEXT NOT NULL, `GuildId` TEXT NOT NULL, PRIMARY KEY(`RoleId`,`GuildId`))");
        }
        catch (Exception e)
        {
            Log.Error(e, "Creating 'UnlockedRoles' table failed");
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUnlocked(IRole grp)
    {
        var result = await _database.Connection.ExecuteScalarAsync<int>(
            FindUnlockedRoleByCompositeId,
            new
            {
                RoleId = grp.Id.ToString(),
                GuildId = grp.Guild.Id.ToString()
            }
        );

        return result > 0;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<IRole> GetUnlocked(IGuild guild)
    {
        using var reader = await _database.Connection.ExecuteReaderAsync(
            FindUnlockedRoleByGuildId,
            new { GuildId = guild.Id.ToString() }
        );

        var items = reader.ToAsyncEnumerable(ParseRole)
              .Where(a => a != null)
              .Select(a => a!)
              .OrderBy(a => a.Name);

        await foreach (var item in items)
            yield return item;

        ValueTask<IRole?> ParseRole(IDataReader reader)
        {
            try
            {
                return ValueTask.FromResult<IRole?>(guild.GetRole(ulong.Parse((string)reader["RoleId"])));
            }
            catch (Exception exception)
            {
                return ValueTask.FromException<IRole?>(exception);
            }
        }
    }

    /// <inheritdoc />
    public async Task Unlock(IRole grp)
    {
        await _database.Connection.ExecuteAsync(
            InsertUnlockSql,
            new
            {
                RoleId = grp.Id.ToString(),
                GuildId = grp.Id.ToString(),
            }
        );
    }

    /// <inheritdoc />
    public async Task Lock(IRole grp)
    {
        await _database.Connection.ExecuteAsync(
            DeleteUnlockSql,
            new
            {
                RoleId = grp.Id.ToString(),
                GuildId = grp.Id.ToString(),
            }
        );
    }
}