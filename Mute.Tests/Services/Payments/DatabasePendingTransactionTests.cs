using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Payment;
using JetBrains.Annotations;
using System.Linq;

namespace Mute.Tests.Services.Payments
{
    [TestClass]
    public class DatabasePendingTransactionTests
    {
        [TestMethod]
        public async Task CreateTransactionDoesNotThrow()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            await pending.CreatePending(0, 1, 10, "GBP", "Note", now);
        }

        private static async Task<(uint, uint, uint, uint)> CreateTestTransactions(DateTime now, [NotNull] IPendingTransactions svc)
        {
            var a = await svc.CreatePending(0, 1, 10, "TEST", "Note 1", now + TimeSpan.FromMinutes(1));
            var b = await svc.CreatePending(0, 1, 1, "TEST2", "Note 3", now + TimeSpan.FromMinutes(3));
            var c = await svc.CreatePending(1, 0, 5, "TEST", "Note 2", now + TimeSpan.FromMinutes(2));
            var d = await svc.CreatePending(1, 2, 5, "TEST3", "Note 4", now + TimeSpan.FromMinutes(3));

            return (a, b, c, d);
        }

        [TestMethod]
        public async Task GetTransactionByDebtId()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (a, _, _, _) = await CreateTestTransactions(now, pending);

            var results = await pending.Get(debtId: a).ToArrayAsync();

            Assert.AreEqual(1, results.Length);
            Assert.AreEqual("test", results[0].Unit);
            Assert.AreEqual("Note 1", results[0].Note);
            Assert.AreEqual((uint)0, results[0].FromId);
            Assert.AreEqual((uint)1, results[0].ToId);
            Assert.IsTrue(results[0].State == PendingState.Pending);
            Assert.AreEqual(a, results[0].Id);
        }

        [TestMethod]
        public async Task GetTransactionByFromId()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (_, _, _, _) = await CreateTestTransactions(now, pending);

            var results = await (pending.Get(fromId: 1)).ToArrayAsync();

            Assert.AreEqual(2, results.Length);
            Assert.AreEqual("test", results[0].Unit);
            Assert.AreEqual("test3", results[1].Unit);
        }

        [TestMethod]
        public async Task GetTransactionByToId()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (_, _, _, _) = await CreateTestTransactions(now, pending);

            var results = await (pending.Get(toId: 1)).ToArrayAsync();

            Assert.AreEqual(2, results.Length);
            Assert.AreEqual("test", results[0].Unit);
            Assert.AreEqual("test2", results[1].Unit);
        }

        [TestMethod]
        public async Task GetTransactionByUnit()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (_, _, _, _) = await CreateTestTransactions(now, pending);

            var results = await (pending.Get(unit: "test")).ToArrayAsync();

            Assert.AreEqual(2, results.Length);
            Assert.AreEqual("Note 1", results[0].Note);
            Assert.AreEqual("Note 2", results[1].Note);
        }

        [TestMethod]
        public async Task GetTransactionByBefore()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (_, _, _, _) = await CreateTestTransactions(now, pending);

            var results = await (pending.Get(before: now + TimeSpan.FromMinutes(2.5))).ToArrayAsync();

            Assert.AreEqual(2, results.Length);
            Assert.AreEqual("test", results[0].Unit);
            Assert.AreEqual("test", results[1].Unit);
        }

        [TestMethod]
        public async Task GetTransactionByAfter()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (_, _, _, _) = await CreateTestTransactions(now, pending);

            var results = await (pending.Get(after: now + TimeSpan.FromMinutes(2.5))).ToArrayAsync();

            Assert.AreEqual(2, results.Length);
            Assert.AreEqual("test2", results[0].Unit);
            Assert.AreEqual("test3", results[1].Unit);
        }

        [TestMethod]
        public async Task GetTransactionByPending()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (a, b, c, d) = await CreateTestTransactions(now, pending);

            var results = await (pending.Get(state: PendingState.Pending)).ToArrayAsync();

            Assert.AreEqual(4, results.Length);
            Assert.AreEqual(a, results[0].Id);
            Assert.AreEqual(c, results[1].Id);
            Assert.AreEqual(b, results[2].Id);
            Assert.AreEqual(d, results[3].Id);
        }

        [TestMethod]
        public async Task ConfirmPendingTransaction()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (a, _, _, _) = await CreateTestTransactions(now, pending);

            var transactionsBefore = await (tsx.GetTransactions(0, 1, "TEST")).ToArrayAsync();
            Assert.AreEqual(0, transactionsBefore.Length);

            Assert.AreEqual(ConfirmResult.Confirmed, await pending.ConfirmPending(a));

            var notConfirmed = await (pending.Get(state: PendingState.Pending)).ToArrayAsync();
            Assert.AreEqual(3, notConfirmed.Length);

            var transactionsAfter = await (tsx.GetTransactions(0, 1, "TEST")).ToArrayAsync();
            Assert.AreEqual(1, transactionsAfter.Length);
        }

        [TestMethod]
        public async Task ConfirmAlreadyConfirmedTransaction()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (a, _, _, _) = await CreateTestTransactions(now, pending);

            Assert.AreEqual(ConfirmResult.Confirmed, await pending.ConfirmPending(a));
            Assert.AreEqual(ConfirmResult.AlreadyConfirmed, await pending.ConfirmPending(a));

            var notConfirmed = await (pending.Get(state: PendingState.Pending)).ToArrayAsync();
            Assert.AreEqual(3, notConfirmed.Length);
        }

        [TestMethod]
        public async Task ConfirmAlreadyDeniedTransaction()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (a, _, _, _) = await CreateTestTransactions(now, pending);

            Assert.AreEqual(DenyResult.Denied, await pending.DenyPending(a));
            Assert.AreEqual(ConfirmResult.AlreadyDenied, await pending.ConfirmPending(a));

            var notConfirmed = await (pending.Get(state: PendingState.Pending)).ToArrayAsync();
            Assert.AreEqual(3, notConfirmed.Length);
        }

        [TestMethod]
        public async Task ConfirmNotExistsTransaction()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (_, _, _, d) = await CreateTestTransactions(now, pending);

            Assert.AreEqual(ConfirmResult.IdNotFound, await pending.ConfirmPending(d + 10));
            
            var notConfirmed = await (pending.Get(state: PendingState.Pending)).ToArrayAsync();
            Assert.AreEqual(4, notConfirmed.Length);
        }

        [TestMethod]
        public async Task DenyPendingTransaction()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (a, _, _, _) = await CreateTestTransactions(now, pending);

            var transactionsBefore = await (tsx.GetTransactions(0, 1, "TEST")).ToArrayAsync();
            Assert.AreEqual(0, transactionsBefore.Length);

            Assert.AreEqual(DenyResult.Denied, await pending.DenyPending(a));

            var notConfirmed = await (pending.Get(state: PendingState.Pending)).ToArrayAsync();
            Assert.AreEqual(3, notConfirmed.Length);

            var transactionsAfter = await (tsx.GetTransactions(0, 1, "TEST")).ToArrayAsync();
            Assert.AreEqual(0, transactionsAfter.Length);
        }

        [TestMethod]
        public async Task DenyAlreadyConfirmedTransaction()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (a, _, _, _) = await CreateTestTransactions(now, pending);

            Assert.AreEqual(ConfirmResult.Confirmed, await pending.ConfirmPending(a));
            Assert.AreEqual(DenyResult.AlreadyConfirmed, await pending.DenyPending(a));

            var notConfirmed = await (pending.Get(state: PendingState.Pending)).ToArrayAsync();
            Assert.AreEqual(3, notConfirmed.Length);
        }

        [TestMethod]
        public async Task DenyAlreadyDeniedTransaction()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (a, _, _, _) = await CreateTestTransactions(now, pending);

            Assert.AreEqual(DenyResult.Denied, await pending.DenyPending(a));
            Assert.AreEqual(DenyResult.AlreadyDenied, await pending.DenyPending(a));

            var notConfirmed = await (pending.Get(state: PendingState.Pending)).ToArrayAsync();
            Assert.AreEqual(3, notConfirmed.Length);
        }

        [TestMethod]
        public async Task DenyNotExistsTransaction()
        {
            var db = new SqliteInMemoryDatabase();
            var tsx = new DatabaseTransactions(db);
            var pending = new DatabasePendingTransactions(db, tsx);

            var now = DateTime.UtcNow;
            var (_, _, _, d) = await CreateTestTransactions(now, pending);

            Assert.AreEqual(DenyResult.IdNotFound, await pending.DenyPending(d + 10));

            var notConfirmed = await (pending.Get(state: PendingState.Pending)).ToArrayAsync();
            Assert.AreEqual(4, notConfirmed.Length);
        }
    }
}
