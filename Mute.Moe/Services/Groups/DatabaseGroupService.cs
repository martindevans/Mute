using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
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

        public async Task<bool> IsUnlocked([NotNull] IRole grp)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = FindUnlockedRoleByCompositeId;
                cmd.Parameters.Add(new SQLiteParameter("@RoleId", System.Data.DbType.String) { Value = grp.Id.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = grp.Guild.Id.ToString() });

                using (var results = await cmd.ExecuteReaderAsync())
                    return results.HasRows;
            }
        }

        [NotNull] public IAsyncEnumerable<IRole> GetUnlocked([NotNull] IGuild guild)
        {
            var cmd = _database.CreateCommand();
            cmd.CommandText = FindUnlockedRoleByGuildId;
            cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = guild.Id.ToString() });

            return new RolesResult(cmd, guild).Where(r => r != null);
        }

        public async Task Unlock(IRole grp)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertUnlockSql;
                cmd.Parameters.Add(new SQLiteParameter("@RoleId", System.Data.DbType.String) { Value = grp.Id.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = grp.Guild.Id.ToString() });
                await cmd.ExecuteNonQueryAsync();
            }
        }

        public async Task Lock(IRole grp)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = DeleteUnlockSql;
                cmd.Parameters.Add(new SQLiteParameter("@RoleId", System.Data.DbType.String) { Value = grp.Id.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@GuildId", System.Data.DbType.String) { Value = grp.Guild.Id.ToString() });
                await cmd.ExecuteNonQueryAsync();
            }
        }

        private class RolesResult
            : IDisposable, IAsyncEnumerable<IRole>
        {
            private readonly DbCommand _command;
            private readonly IGuild _guild;

            protected internal RolesResult(DbCommand command, IGuild guild)
            {
                _command = command;
                _guild = guild;
            }

            public void Dispose()
            {
                _command.Dispose();
            }

            [NotNull]
            IAsyncEnumerator<IRole> IAsyncEnumerable<IRole>.GetEnumerator()
            {
                return new AsyncEnumerator(_command, _guild);
            }

            private class AsyncEnumerator
                : IAsyncEnumerator<IRole>
            {
                private readonly DbCommand _command;
                private readonly IGuild _guild;

                private DbDataReader _reader;

                public AsyncEnumerator(DbCommand command, IGuild guild)
                {
                    _command = command;
                    _guild = guild;
                }

                public void Dispose()
                {
                    _reader.Close();
                }

                public async Task<bool> MoveNext(CancellationToken cancellationToken)
                {
                    if (_reader == null)
                        _reader = await _command.ExecuteReaderAsync(cancellationToken);

                    return await _reader.ReadAsync(cancellationToken);
                }

                public IRole Current => _guild.GetRole(ulong.Parse((string)_reader["RoleId"]));
            }
        }
    }
}
