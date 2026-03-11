using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe;
using Mute.Moe.Services.Database;
using System.Threading.Tasks;

namespace Mute.Tests.Services.Database
{
    [TestClass]
    public class SimpleJsonBlobTableTests
    {
        [TestMethod]
        public async Task PutGet()
        {
            var db = new SqliteDatabase(new Configuration
            {
                Database = new DatabaseConfig
                {
                    ConnectionString = "Data Source=:memory:"
                },
                Agent = null!
            });

            var table = new TestTable(db);

            // Store
            await table.Put(123, new TestData(4, "hello"));

            // Get something else (null)
            var nl = await table.Get(321);
            Assert.IsNull(nl);

            // Check item we stored
            var item1 = await table.Get(123);
            Assert.IsNotNull(item1);
            Assert.AreEqual(4, item1.A);
            Assert.AreEqual("hello", item1.B);

            // Overwrite item we stored
            await table.Put(123, new TestData(5, "world"));

            // Store something else
            await table.Put(111, new TestData(6, ""));

            // Check items we stored
            var item2 = await table.Get(123);
            Assert.IsNotNull(item2);
            Assert.AreEqual(5, item2.A);
            Assert.AreEqual("world", item2.B);

            var item3 = await table.Get(111);
            Assert.IsNotNull(item3);
            Assert.AreEqual(6, item3.A);
            Assert.AreEqual("", item3.B);
        }

        private class TestTable(IDatabaseService db)
            : SimpleJsonBlobTable<TestData>("test_table", db);

        private record TestData(int A, string B);
    }
}
