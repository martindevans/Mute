using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe;
using Mute.Moe.Services.Database;
using System.Threading.Tasks;

namespace Mute.Tests.Services.Database
{
    [TestClass]
    public class SimpleJsonBlobTableTests
    {
        private static IDatabaseService CreateDb()
        {
            return new SqliteDatabase(new Configuration
            {
                Database = new DatabaseConfig
                {
                    ConnectionString = "Data Source=:memory:"
                },
                Agent = null!
            });
        }

        [TestMethod]
        public async Task PutGet()
        {
            var table = new TestTable(CreateDb());

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

        [TestMethod]
        public async Task Delete_ExistingItem_ReturnsTrue()
        {
            var table = new TestTable(CreateDb());

            await table.Put(123, new TestData(4, "hello"));

            var result = await table.Delete(123);
            Assert.IsTrue(result);

            var item = await table.Get(123);
            Assert.IsNull(item);
        }

        [TestMethod]
        public async Task Delete_NonExistingItem_ReturnsFalse()
        {
            var table = new TestTable(CreateDb());

            var result = await table.Delete(999);
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task Clear_RemovesAllItems()
        {
            var table = new TestTable(CreateDb());

            await table.Put(1, new TestData(1, "a"));
            await table.Put(2, new TestData(2, "b"));

            await table.Clear();

            Assert.AreEqual(0L, await table.Count());
        }

        [TestMethod]
        public async Task Count_ReturnsCorrectCount()
        {
            var table = new TestTable(CreateDb());

            Assert.AreEqual(0L, await table.Count());

            await table.Put(1, new TestData(1, "a"));
            Assert.AreEqual(1L, await table.Count());

            await table.Put(2, new TestData(2, "b"));
            Assert.AreEqual(2L, await table.Count());
        }

        [TestMethod]
        public async Task Random_EmptyTable_ReturnsNull()
        {
            var table = new TestTable(CreateDb());

            var result = await table.Random();
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task Random_NonEmptyTable_ReturnsItem()
        {
            var table = new TestTable(CreateDb());

            await table.Put(1, new TestData(1, "a"));

            var result = await table.Random();
            Assert.IsNotNull(result);
        }

        private class TestTable(IDatabaseService db)
            : SimpleJsonBlobTable<TestData>("test_table", db);

        private record TestData(int A, string B);
    }
}
