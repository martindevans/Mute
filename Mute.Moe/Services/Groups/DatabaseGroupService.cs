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
            using var connection = _database.GetConnection();
            connection.Execute("CREATE TABLE IF NOT EXISTS `UnlockedRoles` (`RoleId` TEXT NOT NULL, `GuildId` TEXT NOT NULL, PRIMARY KEY(`RoleId`,`GuildId`))");
        }
        catch (Exception e)
        {
            Log.Error(e, "Creating 'UnlockedRoles' table failed");
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsUnlocked(IRole grp)
    {
        using var connection = _database.GetConnection();

        var result = await connection.ExecuteScalarAsync<int>(
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
    public async Task<IEnumerable<IRole>> GetUnlocked(IGuild guild)
    {
        using var connection = _database.GetConnection();
        var unlockeds = connection.QueryAsync<UnlockedRole>(
            FindUnlockedRoleByGuildId,
            new { GuildId = guild.Id.ToString() }
        );

        return await unlockeds
              .ToAsyncEnumerable()
              .Select(async (u, _, _) => await GetRole(u))
              .Where(a => a != null)
              .Select(a => a!)
              .OrderBy(a => a.Name)
              .ToArrayAsync();

        async ValueTask<IRole?> GetRole(UnlockedRole unlocked)
        {
            try
            {
                return await guild.GetRoleAsync(ulong.Parse(unlocked.RoleId));
            }
            catch (Exception exception)
            {
                Log.Error("Failed to fetch guild role Guild={0} ID={1} Ex={2}", guild.Name, unlocked.RoleId, exception);
                return null;
            }
        }
    }

    /// <inheritdoc />
    public async Task Unlock(IRole grp)
    {
        using var connection = _database.GetConnection();
        await connection.ExecuteAsync(
            InsertUnlockSql,
            new
            {
                RoleId = grp.Id.ToString(),
                GuildId = grp.Guild.Id.ToString(),
            }
        );
    }

    /// <inheritdoc />
    public async Task Lock(IRole grp)
    {
        using var connection = _database.GetConnection();
        await connection.ExecuteAsync(
            DeleteUnlockSql,
            new
            {
                RoleId = grp.Id.ToString(),
                GuildId = grp.Guild.Id.ToString(),
            }
        );
    }

    private record UnlockedRole(string RoleId, string GuildId);
}