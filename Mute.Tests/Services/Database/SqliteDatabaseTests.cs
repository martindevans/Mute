using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe;
using Mute.Moe.Services.Database;

namespace Mute.Tests.Services.Database
{
    [TestClass]
    public class SqliteDatabaseTests
    {
        [TestMethod]
        public void CreateDatabase()
        {
            var db = new SqliteDatabase(new Configuration {
                Database = new DatabaseConfig {
                    ConnectionString = "Data Source=:memory:"
                }
            });

            using (var cmd = db.CreateCommand())
            {
                cmd.CommandText = "PRAGMA VACUUM";
                cmd.ExecuteNonQuery();
            }
        }

        [TestMethod]
        public void Exec()
        {
            var db = new SqliteDatabase(new Configuration {
                Database = new DatabaseConfig {
                    ConnectionString = "Data Source=:memory:"
                }
            });

            db.Exec("CREATE TABLE `test` (`foo` TEXT NOT NULL)");
        }
    }
}
