#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Payment;
using System.Linq;
using JetBrains.Annotations;

namespace Mute.Tests.Services.Payments;

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

    private static async Task<(uint, uint, uint, uint)> CreateTestTransactions(DateTime now, IPendingTransactions svc)
    {
        var a = await svc.CreatePending(0, 1, 10, "TEST", "Note 1", now + TimeSpan.FromMinutes(1));
        var b = await svc.CreatePending(0, 1, 1, "TEST2", "Note 3", now + TimeSpan.FromMinutes(3));
        var c = await svc.CreatePending(1, 0, 5, "TEST", "Note 2", now + TimeSpan.FromMinutes(2));
        var d = await svc.CreatePending(1, 2, 5, "TEST3", "Note 4", now + TimeSpan.FromMinutes(3));

        return (a.Id, b.Id, c.Id, d.Id);
    }

    [TestMethod]
    public async Task GetTransactionByDebtId()
    {
        var db = new SqliteInMemoryDatabase();
        var tsx = new DatabaseTransactions(db);
        var pending = new DatabasePendingTransactions(db, tsx);

        var now = DateTime.UtcNow;
        var (a, _, _, _) = await CreateTestTransactions(now, pending);

        var results = (await pending.Get(debtId: a)).ToArray();
        var result = await pending.GetSingle(debtId: a);

        Assert.HasCount(1, results);
        Assert.AreEqual("test", results[0].Unit);
        Assert.AreEqual("Note 1", results[0].Note);
        Assert.AreEqual((uint)0, results[0].FromId);
        Assert.AreEqual((uint)1, results[0].ToId);
        Assert.AreEqual(PendingState.Pending, results[0].State);
        Assert.AreEqual(a, results[0].Id);

        Assert.AreEqual(result, results[0]);
    }

    [TestMethod]
    public async Task GetTransactionByFromId()
    {
        var db = new SqliteInMemoryDatabase();
        var tsx = new DatabaseTransactions(db);
        var pending = new DatabasePendingTransactions(db, tsx);

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, pending);

        var results = (await pending.Get(fromId: 1)).ToArray();

        Assert.HasCount(2, results);
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
        await CreateTestTransactions(now, pending);

        var results = (await pending.Get(toId: 1)).ToArray();

        Assert.HasCount(2, results);
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
        await CreateTestTransactions(now, pending);

        var results = (await pending.Get(unit: "test")).ToArray();

        Assert.HasCount(2, results);
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
        await CreateTestTransactions(now, pending);

        var results = (await pending.Get(before: now + TimeSpan.FromMinutes(2.5))).ToArray();

        Assert.HasCount(2, results);
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
        await CreateTestTransactions(now, pending);

        var results = (await pending.Get(after: now + TimeSpan.FromMinutes(2.5))).ToArray();

        Assert.HasCount(2, results);
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

        var results = (await pending.Get(state: PendingState.Pending)).ToArray();

        Assert.HasCount(4, results);
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

        var transactionsBefore = (await tsx.GetTransactions(0, 1, "TEST")).ToArray();
        Assert.HasCount(0, transactionsBefore);

        Assert.AreEqual(ConfirmResult.Confirmed, await pending.ConfirmPending(a));

        var notConfirmed = (await pending.Get(state: PendingState.Pending)).ToArray();
        Assert.HasCount(3, notConfirmed);

        var transactionsAfter = (await tsx.GetTransactions(0, 1, "TEST")).ToArray();
        Assert.HasCount(1, transactionsAfter);
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

        var notConfirmed = (await pending.Get(state: PendingState.Pending)).ToArray();
        Assert.HasCount(3, notConfirmed);
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

        var notConfirmed = (await pending.Get(state: PendingState.Pending)).ToArray();
        Assert.HasCount(3, notConfirmed);
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
            
        var notConfirmed = (await pending.Get(state: PendingState.Pending)).ToArray();
        Assert.HasCount(4, notConfirmed);
    }

    [TestMethod]
    public async Task DenyPendingTransaction()
    {
        var db = new SqliteInMemoryDatabase();
        var tsx = new DatabaseTransactions(db);
        var pending = new DatabasePendingTransactions(db, tsx);

        var now = DateTime.UtcNow;
        var (a, _, _, _) = await CreateTestTransactions(now, pending);

        var transactionsBefore = (await tsx.GetTransactions(0, 1, "TEST")).ToArray();
        Assert.HasCount(0, transactionsBefore);

        Assert.AreEqual(DenyResult.Denied, await pending.DenyPending(a));

        var notConfirmed = (await pending.Get(state: PendingState.Pending)).ToArray();
        Assert.HasCount(3, notConfirmed);

        var transactionsAfter = (await tsx.GetTransactions(0, 1, "TEST")).ToArray();
        Assert.HasCount(0, transactionsAfter);
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

        var notConfirmed = (await pending.Get(state: PendingState.Pending)).ToArray();
        Assert.HasCount(3, notConfirmed);
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

        var notConfirmed = (await pending.Get(state: PendingState.Pending)).ToArray();
        Assert.HasCount(3, notConfirmed);
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

        var notConfirmed = (await pending.Get(state: PendingState.Pending)).ToArray();
        Assert.HasCount(4, notConfirmed);
    }

    [TestMethod]
    public async Task GetTransactionByConfirmedState()
    {
        var db = new SqliteInMemoryDatabase();
        var tsx = new DatabaseTransactions(db);
        var pending = new DatabasePendingTransactions(db, tsx);

        var now = DateTime.UtcNow;
        var (a, b, _, _) = await CreateTestTransactions(now, pending);

        await pending.ConfirmPending(a);
        await pending.ConfirmPending(b);

        var confirmed = (await pending.Get(state: PendingState.Confirmed)).ToArray();
        Assert.HasCount(2, confirmed);
        Assert.IsTrue(confirmed.All(t => t.State == PendingState.Confirmed));
    }

    [TestMethod]
    public async Task GetTransactionByDeniedState()
    {
        var db = new SqliteInMemoryDatabase();
        var tsx = new DatabaseTransactions(db);
        var pending = new DatabasePendingTransactions(db, tsx);

        var now = DateTime.UtcNow;
        var (a, _, c, _) = await CreateTestTransactions(now, pending);

        await pending.DenyPending(a);
        await pending.DenyPending(c);

        var denied = (await pending.Get(state: PendingState.Denied)).ToArray();
        Assert.HasCount(2, denied);
        Assert.IsTrue(denied.All(t => t.State == PendingState.Denied));
    }

    [TestMethod]
    public async Task ConfirmedTransaction_PreservesAllFields()
    {
        var db = new SqliteInMemoryDatabase();
        var tsx = new DatabaseTransactions(db);
        var pending = new DatabasePendingTransactions(db, tsx);

        var now = DateTime.UtcNow.AddSeconds(-1); // truncate sub-second precision
        now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Utc);

        var tsx2 = await pending.CreatePending(42, 99, 12.5m, "GBP", "TestNote", now);
        var id = tsx2.Id;
        Assert.AreEqual(ConfirmResult.Confirmed, await pending.ConfirmPending(id));

        var confirmed = (await tsx.GetTransactions(fromId: 42, toId: 99, unit: "gbp")).ToArray();
        Assert.HasCount(1, confirmed);

        var t = confirmed[0];
        Assert.AreEqual((ulong)42, t.FromId);
        Assert.AreEqual((ulong)99, t.ToId);
        Assert.AreEqual(12.5m, t.Amount);
        Assert.AreEqual("gbp", t.Unit);
        Assert.AreEqual("TestNote", t.Note);
        Assert.AreEqual(now, t.Instant);
    }

    [TestMethod]
    public async Task CreatePending_NegativeAmount_Throws()
    {
        var db = new SqliteInMemoryDatabase();
        var tsx = new DatabaseTransactions(db);
        var pending = new DatabasePendingTransactions(db, tsx);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => pending.CreatePending(0, 1, -5, "GBP", null, DateTime.UtcNow));
    }

    [TestMethod]
    public async Task CreatePending_SelfTransaction_Throws()
    {
        var db = new SqliteInMemoryDatabase();
        var tsx = new DatabaseTransactions(db);
        var pending = new DatabasePendingTransactions(db, tsx);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => pending.CreatePending(0, 0, 10, "GBP", null, DateTime.UtcNow));
    }

    [TestMethod]
    public void Constructor_RequiresDatabaseTransactions()
    {
        var db = new SqliteInMemoryDatabase();
        var fakeTsx = new FakeTransactions();

        Assert.Throws<ArgumentException>(
            () => new DatabasePendingTransactions(db, fakeTsx));
    }

    [TestMethod]
    public async Task Pending_UnitStoredAsLowercase()
    {
        var db = new SqliteInMemoryDatabase();
        var tsx = new DatabaseTransactions(db);
        var pending = new DatabasePendingTransactions(db, tsx);

        var tsx2 = await pending.CreatePending(0, 1, 10, "GBP", null, DateTime.UtcNow);
        var id = tsx2.Id;
        var results = (await pending.Get(debtId: id)).ToArray();

        Assert.HasCount(1, results);
        Assert.AreEqual("gbp", results[0].Unit);
    }

    public TestContext TestContext { get; [UsedImplicitly] set; } = null!;
}

/// <summary>
/// A fake ITransactions implementation (not DatabaseTransactions) used to test constructor validation.
/// </summary>
internal class FakeTransactions
    : ITransactions
{
    public Task CreateTransaction(ulong fromId, ulong toId, decimal amount, string unit, string? note, DateTime instant)
        => Task.CompletedTask;

    public async Task<IEnumerable<Transaction>> GetTransactions(ulong? fromId = null, ulong? toId = null, string? unit = null, DateTime? after = null, DateTime? before = null)
    {
        return Array.Empty<Transaction>();
    }
}