using System.Data.Common;
using System.Data.SQLite;
using Mute.Services;

namespace Mute.Tests.Mocks
{
    public class DatabaseServiceInMemory
        : IDatabaseService
    {
        private readonly SQLiteConnection _connection;

        public DatabaseServiceInMemory()
        {
            _connection = new SQLiteConnection("Data Source=:memory:");
            _connection.Open();
        }

        public DbCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }
    }
}
