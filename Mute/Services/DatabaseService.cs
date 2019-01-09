using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Services
{
    public class DatabaseService
        : IDatabaseService
    {
        private readonly SQLiteConnection _dbConnection;

        public DatabaseService([NotNull] Configuration config)
        {
            _dbConnection = new SQLiteConnection(config.Database.ConnectionString);
            _dbConnection.Open();
        }

        [NotNull] public DbCommand CreateCommand()
        {
            return new SQLiteCommand(_dbConnection);
        }
    }

    public static class IDatabaseServiceExtensions
    {
        public static int Exec([NotNull] this IDatabaseService db, string sql)
        {
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }

        public static Task<DbDataReader> ExecReader([NotNull] this IDatabaseService db, string sql)
        {
            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteReaderAsync();
            }
        }
    }

    public interface IDatabaseService
    {
        DbCommand CreateCommand();
    }
}
