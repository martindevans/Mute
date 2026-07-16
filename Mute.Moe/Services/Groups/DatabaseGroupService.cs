using Discord;
using Mute.Moe.Services.Database;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Mute.Moe.Services.Groups;

/// <inheritdoc />
public partial class DatabaseGroupService
    : IGroups
{
    #region SQL
    private const string FindUnlockedRoleByCompositeId = "SELECT 1 FROM UnlockedRoles WHERE RoleId = @RoleId AND GuildId = @GuildId";
    private const string FindUnlockedRoleByGuildId = "SELECT * FROM UnlockedRoles WHERE GuildId = @GuildId";

    private const string InsertUnlockSql = "INSERT OR IGNORE into UnlockedRoles (RoleId, GuildId) values(@RoleId, @GuildId)";
    private const string DeleteUnlockSql = "Delete from UnlockedRoles Where RoleId = @RoleId AND GuildId = @GuildId";
    #endregion

    private readonly ILogger<DatabaseGroupService> _logger;
    private readonly IDatabaseService _database;

    /// <summary>
    /// Create a new <see cref="DatabaseGroupService"/>
    /// </summary>
    /// <param name="database"></param>
    /// <param name="logger"></param>
    public DatabaseGroupService(IDatabaseService database, ILogger<DatabaseGroupService> logger)
    {
        _database = database;
        _logger = logger;

        // Create database structure
        using var connection = _database.GetConnection();
        connection.Execute("CREATE TABLE IF NOT EXISTS `UnlockedRoles` (`RoleId` TEXT NOT NULL, `GuildId` TEXT NOT NULL, PRIMARY KEY(`RoleId`,`GuildId`))");
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
              .Select(async (u, _, _) => await GetRole(u))  // Get the role object from Discord
              .Where(a => a != null)                        // Ignore null results
              .Select(a => a!)                              // Declare that it's not null (we just checked)
              .OrderBy(a => a.Name)                         // Order by name
              .ToArrayAsync();

        async ValueTask<IRole?> GetRole(UnlockedRole unlocked)
        {
            try
            {
                return await guild.GetRoleAsync(ulong.Parse(unlocked.RoleId));
            }
            catch (Exception exception)
            {
                LogFailedToFetchRole(guild.Name, unlocked.RoleId, exception);
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

    #region logging
    [LoggerMessage(LogLevel.Error, "Failed to fetch guild role Guild={guild} ID={role}")]
    private partial void LogFailedToFetchRole(string guild, string role, Exception ex);
    #endregion
}