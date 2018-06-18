using System.Data.Common;
using System.Data.SQLite;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Services
{
    public class DatabaseService
    {
        private readonly SQLiteConnection _dbConnection;

        public DatabaseService([NotNull] DatabaseConfig config)
        {
            _dbConnection = new SQLiteConnection(config.ConnectionString);
            _dbConnection.Open();
        }

        public DbCommand CreateCommand()
        {
            return new SQLiteCommand(_dbConnection);
        }

        public int Exec(string sql)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteNonQuery();
            }
        }

        public Task<DbDataReader> ExecReader(string sql)
        {
            using (var cmd = CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteReaderAsync();
            }
        }
    }
}
