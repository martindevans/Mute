using Mute.Moe.Services.Database;
using System;
using System.Data.Common;
using System.Data.SQLite;
using System.Linq;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Steam
{
    public class SteamLightweightAppInfoDbCache
        : ISteamLightweightAppInfoStorage
    {
        private readonly IDatabaseService _database;
        private readonly ISteamInfo _steam;

        private const string InsertSql = "INSERT into SteamLightweightAppInfo (AppId, Name, Desc) values(@AppId, @Name, @Desc)";
        private const string GetSql = "SELECT * FROM SteamLightweightAppInfo WHERE appId = @AppId";

        public SteamLightweightAppInfoDbCache(IDatabaseService database, ISteamInfo steam)
        {
            _database = database;
            _steam = steam;

            try
            {
                _database.Exec("CREATE TABLE IF NOT EXISTS `SteamLightweightAppInfo` (`AppId` TEXT NOT NULL, `Name` TEXT NOT NULL, `Desc` TEXT NOT NULL)");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public async Task<ILightweightAppInfoModel?> Get(uint appId)
        {
            static LightweightAppInfo Parse(DbDataReader reader)
            {
                var appid = uint.Parse(reader["AppId"].ToString()!);
                var name = reader["Name"].ToString()!;
                var desc = reader["Desc"].ToString()!;

                return new LightweightAppInfo(appid, name, desc);
            }

            DbCommand PrepareQuery(IDatabaseService db)
            {
                var cmd = db.CreateCommand();
                cmd.CommandText = GetSql;
                cmd.Parameters.Add(new SQLiteParameter("@AppId", System.Data.DbType.String) { Value = appId.ToString() });
                return cmd;
            }

            var result = await new SqlAsyncResult<ILightweightAppInfoModel?>(_database, PrepareQuery, Parse).SingleOrDefaultAsync();
            if (result != null)
                return result;

            var fullInfo = await _steam.GetStoreInfoSlow(appId);
            if (fullInfo == null)
                return null;

            await using var cmd = _database.CreateCommand();
            cmd.CommandText = InsertSql;
            cmd.Parameters.Add(new SQLiteParameter("@AppId", System.Data.DbType.String) { Value = appId.ToString() });
            cmd.Parameters.Add(new SQLiteParameter("@Name", System.Data.DbType.String) { Value = fullInfo.Name });
            cmd.Parameters.Add(new SQLiteParameter("@Desc", System.Data.DbType.String) { Value = fullInfo.ShortDescription });
            await cmd.ExecuteNonQueryAsync();

            return new LightweightAppInfo(appId, fullInfo.Name, fullInfo.ShortDescription);
        }

        private class LightweightAppInfo
            : ILightweightAppInfoModel
        {
            public uint AppId { get; }

            public string Name { get; }

            public string ShortDescription { get; }

            public LightweightAppInfo(uint appid, string name, string shortDescription)
            {
                AppId = appid;
                Name = name;
                ShortDescription = shortDescription;
            }
        }
    }
}
