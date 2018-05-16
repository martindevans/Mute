using System.Data.Common;
using System.Data.SQLite;

namespace Mute.Services
{
    public class DatabaseService
    {
        private readonly SQLiteConnection _dbConnection;

        public DatabaseService(DatabaseConfig config)
        {
            _dbConnection = new SQLiteConnection(config.ConnectionString);
            _dbConnection.Open();
        }

        public DbCommand CreateCommand()
        {
            return new SQLiteCommand(_dbConnection);
        }
    }
}
