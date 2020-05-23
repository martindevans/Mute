using System.Data.Common;
using System.Data.SQLite;


namespace Mute.Moe.Services.Database
{
    public class SqliteDatabase
        : IDatabaseService
    {
        private readonly SQLiteConnection _dbConnection;

        public SqliteDatabase( Configuration config)
        {
            _dbConnection = new SQLiteConnection(config.Database.ConnectionString);
            _dbConnection.Open();
        }

         public DbCommand CreateCommand()
        {
            return new SQLiteCommand(_dbConnection);
        }
    }

    // ReSharper disable once InconsistentNaming
    public static class IDatabaseServiceExtensions
    {
        public static int Exec( this IDatabaseService db, string sql)
        {
            using var cmd = db.CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }
    }
}
