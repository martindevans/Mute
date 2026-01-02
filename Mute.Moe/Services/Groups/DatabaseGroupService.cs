using Discord;
using Mute.Moe.Services.Database;
using Serilog;
using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;

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
    public async Task<bool> IsUnlocked( IRole grp)
    {
        await using var cmd = _database.CreateCommand();
        cmd.CommandText = FindUnlockedRoleByCompositeId;
        cmd.Parameters.Add(new SQLiteParameter("@RoleId", System.Data.DbType.String) { Value = grp.Id.ToString() });
        cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = grp.Guild.Id.ToString() });

        await using var results = await cmd.ExecuteReaderAsync();
        return results.HasRows;
    }

    /// <inheritdoc />
    public IAsyncEnumerable<IRole> GetUnlocked(IGuild guild)
    {
        return new SqlAsyncResult<IRole?>(_database, PrepareQuery, ParseRole)
              .Where(a => a != null)
              .Select(a => a!)
              .OrderBy(a => a.Name);

        DbCommand PrepareQuery(IDatabaseService db)
        {
            var cmd = _database.CreateCommand();
            cmd.CommandText = FindUnlockedRoleByGuildId;
            cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild.Id.ToString() });
            return cmd;
        }

        IRole? ParseRole(DbDataReader reader)
        {
            return guild.GetRole(ulong.Parse((string)reader["RoleId"]));
        }
    }

    /// <inheritdoc />
    public async Task Unlock(IRole grp)
    {
        await using var cmd = _database.CreateCommand();
        cmd.CommandText = InsertUnlockSql;
        cmd.Parameters.Add(new SQLiteParameter("@RoleId", System.Data.DbType.String) { Value = grp.Id.ToString() });
        cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = grp.Guild.Id.ToString() });
        await cmd.ExecuteNonQueryAsync();
    }

    /// <inheritdoc />
    public async Task Lock(IRole grp)
    {
        await using var cmd = _database.CreateCommand();
        cmd.CommandText = DeleteUnlockSql;
        cmd.Parameters.Add(new SQLiteParameter("@RoleId", System.Data.DbType.String) { Value = grp.Id.ToString() });
        cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = grp.Guild.Id.ToString() });
        await cmd.ExecuteNonQueryAsync();
    }
}