using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Database;

namespace Mute.Tests.Services.Database;

[TestClass]
public class SqliteDatabaseTests
{
    [TestMethod]
    public void CreateDatabase()
    {
        var db = new SqliteInMemoryDatabase();

        using var conn = db.GetConnection();
        conn.Execute("PRAGMA VACUUM");
    }

    [TestMethod]
    public void Exec()
    {
        var db = new SqliteInMemoryDatabase();

        using var conn = db.GetConnection();
        conn.Execute("CREATE TABLE `test` (`foo` TEXT NOT NULL)");
    }
}