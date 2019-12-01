using Mute.Moe.Services.Database;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Steam
{
    public class SteamIdDatabaseStorage
        : ISteamIdStorage
    {
        private IDatabaseService _database;

        private const string InsertIdSql = "INSERT into SteamIds (DiscordId, SteamId) values(@DiscordId, @SteamId)";
        private const string GetIdSql = "SELECT * FROM SteamIds WHERE DiscordId = @DiscordId";

        public SteamIdDatabaseStorage(IDatabaseService database)
        {
            _database = database;

            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `SteamIds` (`DiscordId` TEXT NOT NULL, `SteamId` TEXT NOT NULL)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task<ulong?> Get(ulong discordId)
        {
            ulong? ParseId(DbDataReader reader)
            {
                var data = reader["SteamId"].ToString();
                return ulong.Parse(data);
            }

            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = GetIdSql;
                cmd.Parameters.Add(new SQLiteParameter("@DiscordId", System.Data.DbType.String) { Value = discordId.ToString() });
                return cmd;
            }

            return await ((IAsyncEnumerable<ulong?>)new SqlAsyncResult<ulong?>(_database, PrepareQuery, ParseId)).SingleOrDefault();
        }

        public async Task Set(ulong discordId, ulong steamId)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = InsertIdSql;
                cmd.Parameters.Add(new SQLiteParameter("@DiscordId", System.Data.DbType.String) { Value = discordId.ToString() });
                cmd.Parameters.Add(new SQLiteParameter("@SteamId", System.Data.DbType.String) { Value = steamId.ToString() });

                await cmd.ExecuteNonQueryAsync();
            }
        }
    }
}
