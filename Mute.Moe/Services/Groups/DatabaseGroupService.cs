using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;
using Discord;

using Mute.Moe.Services.Database;

namespace Mute.Moe.Services.Groups
{
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
                Console.WriteLine(e);
            }
        }

        public async Task<bool> IsUnlocked( IRole grp)
        {
            await using var cmd = _database.CreateCommand();
            cmd.CommandText = FindUnlockedRoleByCompositeId;
            cmd.Parameters.Add(new SQLiteParameter("@RoleId", System.Data.DbType.String) { Value = grp.Id.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = grp.Guild.Id.ToString() });

            await using var results = await cmd.ExecuteReaderAsync();
            return results.HasRows;
        }

         public IAsyncEnumerable<IRole> GetUnlocked( IGuild guild)
        {
            IRole ParseRole(DbDataReader reader)
            {
                return guild.GetRole(ulong.Parse((string)reader["RoleId"]));
            }

            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = _database.CreateCommand();
                cmd.CommandText = FindUnlockedRoleByGuildId;
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild.Id.ToString() });
                return cmd;
            }

            return new SqlAsyncResult<IRole>(_database, PrepareQuery, ParseRole).OrderBy(a => a.Name);
        }

        public async Task Unlock(IRole grp)
        {
            await using var cmd = _database.CreateCommand();
            cmd.CommandText = InsertUnlockSql;
            cmd.Parameters.Add(new SQLiteParameter("@RoleId", System.Data.DbType.String) { Value = grp.Id.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = grp.Guild.Id.ToString() });
            await cmd.ExecuteNonQueryAsync();
        }

        public async Task Lock(IRole grp)
        {
            await using var cmd = _database.CreateCommand();
            cmd.CommandText = DeleteUnlockSql;
            cmd.Parameters.Add(new SQLiteParameter("@RoleId", System.Data.DbType.String) { Value = grp.Id.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = grp.Guild.Id.ToString() });
            await cmd.ExecuteNonQueryAsync();
        }
    }
}
