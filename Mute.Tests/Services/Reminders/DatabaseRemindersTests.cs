using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Reminders;

namespace Mute.Tests.Services.Reminders
{
    [TestClass]
    public class DatabaseRemindersTests
    {
        [TestMethod]
        public async Task CreateReminderDoesNotThrow()
        {
            var db = new SqliteInMemoryDatabase();
            var rm = new DatabaseReminders(db);

            var t = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            var r = await rm.Create(t, "pre", "msg", 17, 28);

            Assert.AreEqual(t, r.TriggerTime);
            Assert.AreEqual("pre", r.Prelude);
            Assert.AreEqual("msg", r.Message);
            Assert.AreEqual(17u, r.ChannelId);
            Assert.AreEqual(28u, r.UserId);
        }

        [TestMethod]
        public async Task GetByUserId()
        {
            var db = new SqliteInMemoryDatabase();
            var rm = new DatabaseReminders(db);

            var t = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            var r = await rm.Create(t, "pre", "msg", 17, 28);

            var rs = await rm.Get(userId: 28).ToArray();

            Assert.AreEqual(r, rs.Single());
        }

        [TestMethod]
        public async Task GetByMinTime()
        {
            var db = new SqliteInMemoryDatabase();
            var rm = new DatabaseReminders(db);

            var t = DateTime.UtcNow;
            var r1 = await rm.Create(t, "pre", "msg2", 17, 28);
            var r2 = await rm.Create(t + TimeSpan.FromHours(1), "pre", "msg2", 17, 28);

            var rs = await rm.Get(after: DateTime.UtcNow + TimeSpan.FromHours(0.5f)).ToArray();

            Assert.AreEqual(r2, rs.Single());
        }

        [TestMethod]
        public async Task GetByMaxTime()
        {
            var db = new SqliteInMemoryDatabase();
            var rm = new DatabaseReminders(db);

            var t = DateTime.UtcNow;
            var r1 = await rm.Create(t, "pre", "msg2", 17, 28);
            var r2 = await rm.Create(t + TimeSpan.FromHours(1), "pre", "msg2", 17, 28);

            var rs = await rm.Get(before: t + TimeSpan.FromHours(0.5f)).ToArray();

            Assert.AreEqual(r1, rs.Single());
        }

        [TestMethod]
        public async Task GetByChannel()
        {
            var db = new SqliteInMemoryDatabase();
            var rm = new DatabaseReminders(db);

            var t = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            var r = await rm.Create(t, "pre", "msg", 17, 28);

            var rs = await rm.Get(channel: 17).ToArray();

            Assert.AreEqual(r, rs.Single());
        }

        [TestMethod]
        public async Task GetWithLimit()
        {
            var db = new SqliteInMemoryDatabase();
            var rm = new DatabaseReminders(db);

            var t = DateTime.UtcNow;
            for (var i = 0; i < 10; i++)
                await rm.Create(t + TimeSpan.FromMinutes(i), "pre", "msg " + i, 17, 28);

            var rs = await rm.Get(channel: 17, count: 3).ToArray();

            Assert.AreEqual(3, rs.Length);
            Assert.AreEqual((t + TimeSpan.FromMinutes(0)).UnixTimestamp(), rs[0].TriggerTime.UnixTimestamp());
            Assert.AreEqual((t + TimeSpan.FromMinutes(1)).UnixTimestamp(), rs[1].TriggerTime.UnixTimestamp());
            Assert.AreEqual((t + TimeSpan.FromMinutes(2)).UnixTimestamp(), rs[2].TriggerTime.UnixTimestamp());
        }

        [TestMethod]
        public async Task Delete()
        {
            var db = new SqliteInMemoryDatabase();
            var rm = new DatabaseReminders(db);

            var t = DateTime.UtcNow + TimeSpan.FromMinutes(1);
            var r = await rm.Create(t, "pre", "msg", 17, 28);

            Assert.IsTrue(await rm.Delete(r.ID));

            var rs = await rm.Get(channel: 17).ToArray();

            Assert.AreEqual(0, rs.Length);
        }
    }
}
