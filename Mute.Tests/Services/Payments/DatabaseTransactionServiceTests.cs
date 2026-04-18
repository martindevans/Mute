using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Payment;

namespace Mute.Tests.Services.Payments;

[TestClass]
public class DatabaseTransactionServiceTests
{
    [TestMethod]
    public async Task CreateTransactionDoesNotThrow()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await svc.CreateTransaction(0, 1, 10, "TEST", "Note 1", now);
    }

    private static async Task CreateTestTransactions(DateTime now, [NotNull] ITransactions svc)
    {
        await svc.CreateTransaction(0, 1, 10, "TEST", "Note 1", now + TimeSpan.FromMinutes(1));
        await svc.CreateTransaction(0, 1, 1, "TEST2", "Note 3", now + TimeSpan.FromMinutes(3));

        await svc.CreateTransaction(1, 0, 5, "TEST", "Note 2", now + TimeSpan.FromMinutes(2));

        await svc.CreateTransaction(1, 2, 5, "TEST3", "Note 4", now + TimeSpan.FromMinutes(3));
    }

    //[TestMethod]
    //public async Task GetAllTransactions()
    //{
    //    var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

    //    var now = DateTime.UtcNow;
    //    await CreateTestTransactions(now, svc);

    //    var tsx = await svc.GetAllTransactions(0);

    //    Assert.AreEqual(3, tsx.Count);
    //    Assert.AreEqual("test", tsx[0].Unit);
    //    Assert.AreEqual("Note 2", tsx[1].Note);
    //}

    [TestMethod]
    public async Task GetTransactionByFromId()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var tsx = (await svc.GetTransactions(fromId: 0)).OrderBy(a => a.Instant).ToArray();

        Assert.HasCount(2, tsx);
        Assert.AreEqual("test", tsx[0].Unit);
        Assert.AreEqual("test2", tsx[1].Unit);
    }

    [TestMethod]
    public async Task GetTransactionByToId()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var tsx = (await svc.GetTransactions(toId: 0)).OrderBy(a => a.Instant).ToArray();

        Assert.HasCount(1, tsx);
        Assert.AreEqual("test", tsx[0].Unit);
    }

    [TestMethod]
    public async Task GetTransactionByUnit()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var tsx = (await svc.GetTransactions(unit: "test")).OrderBy(a => a.Instant).ToArray();

        Assert.HasCount(2, tsx);
        Assert.AreEqual("test", tsx[0].Unit);
        Assert.AreEqual("test", tsx[1].Unit);
    }

    [TestMethod]
    public async Task GetTransactionByBefore()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var tsx = (await svc.GetTransactions(before: now + TimeSpan.FromMinutes(2.5))).OrderBy(a => a.Instant).ToArray();

        Assert.HasCount(2, tsx);
        Assert.AreEqual("test", tsx[0].Unit);
        Assert.AreEqual("test", tsx[1].Unit);
    }

    [TestMethod]
    public async Task GetTransactionByAfter()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var tsx = (await svc.GetTransactions(after: now + TimeSpan.FromMinutes(2.5))).OrderBy(a => a.Instant).ToArray();

        Assert.HasCount(2, tsx);
        Assert.AreEqual("test2", tsx[0].Unit);
        Assert.AreEqual("test3", tsx[1].Unit);
    }

    [TestMethod]
    public async Task GetBalances01()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var balances = (await svc.GetBalances(0, 1)).ToArray();

        Assert.HasCount(2, balances);
    }

    [TestMethod]
    public async Task GetBalances10()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var balances = (await svc.GetBalances(1, 0)).ToArray();

        Assert.HasCount(2, balances);
    }

    [TestMethod]
    public async Task GetBalances19()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var balances = (await svc.GetBalances(1, null)).ToArray();

        Assert.HasCount(3, balances);

        //1 -> 0 == -5 TEST
        //1 -> 0 == -1 TEST2
        //1 -> 2 == 5 TEST3

        Assert.AreEqual(-5, balances[0].Amount);
        Assert.AreEqual("test", balances[0].Unit);

        Assert.AreEqual(-1, balances[1].Amount);
        Assert.AreEqual("test2", balances[1].Unit);

        Assert.AreEqual(5, balances[2].Amount);
        Assert.AreEqual("test3", balances[2].Unit);
    }

    [TestMethod]
    public async Task GetBalances01_Values()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var balances = (await svc.GetBalances(0, 1)).OrderBy(b => b.Amount).ToArray();

        // Transactions involving 0 and 1:
        // 0 -> 1: 10 TEST (A=0 owes B=1, positive)
        // 1 -> 0: 5 TEST  (A=0 is owed by B=1, negative from 1's perspective = positive from 0's perspective? No:)
        // From user 0's perspective: 0 gave 10 TEST to 1, got back 5 TEST from 1
        // Net balance in TEST: +10 - 5 = +5 (1 owes 0 5 TEST)
        // 0 -> 1: 1 TEST2 (A=0 owes B=1)
        // Net balance in TEST2: +1

        Assert.HasCount(2, balances);

        Assert.AreEqual(1, balances[0].Amount);
        Assert.AreEqual("test2", balances[0].Unit);
        Assert.AreEqual((ulong)0, balances[0].UserA);

        Assert.AreEqual(5, balances[1].Amount);
        Assert.AreEqual("test", balances[1].Unit);
        Assert.AreEqual((ulong)0, balances[1].UserA);
    }

    [TestMethod]
    public async Task GetBalances10_Values()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var balances = (await svc.GetBalances(1, 0)).OrderBy(b => b.Amount).ToArray();

        // From user 1's perspective: 1 received 10 TEST from 0, gave back 5 TEST to 0
        // Net TEST = -10 + 5 = -5 (1 owes 0 5 TEST)
        // Net TEST2 = -1

        Assert.HasCount(2, balances);

        Assert.AreEqual(-5, balances[0].Amount);
        Assert.AreEqual("test", balances[0].Unit);
        Assert.AreEqual((ulong)1, balances[0].UserA);

        Assert.AreEqual(-1, balances[1].Amount);
        Assert.AreEqual("test2", balances[1].Unit);
        Assert.AreEqual((ulong)1, balances[1].UserA);
    }

    [TestMethod]
    public async Task GetBalances_ZeroNetBalance_Excluded()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        // User 0 pays user 1 exactly 10 GBP, then user 1 pays user 0 exactly 10 GBP
        await svc.CreateTransaction(0, 1, 10, "GBP", "Payment", now);
        await svc.CreateTransaction(1, 0, 10, "GBP", "Repayment", now);

        var balances = (await svc.GetBalances(0, 1)).ToArray();

        // Net balance is zero, so it should be excluded
        Assert.HasCount(0, balances);
    }

    [TestMethod]
    public async Task GetTransactions_NoFilter_ReturnsAll()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var tsx = (await svc.GetTransactions()).ToArray();

        Assert.HasCount(4, tsx);
    }

    [TestMethod]
    public async Task GetAllTransactions_BothDirections()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        // User 0 is involved in 3 transactions: 0->1 (TEST), 0->1 (TEST2), 1->0 (TEST)
        var tsx = await svc.GetAllTransactions(0);

        Assert.HasCount(3, tsx);
    }

    [TestMethod]
    public async Task GetAllTransactions_OrderedByDescendingInstant()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        var tsx = await svc.GetAllTransactions(0);

        Assert.HasCount(3, tsx);
        // Should be ordered by descending instant
        for (var i = 0; i < tsx.Count - 1; i++)
            Assert.IsTrue(tsx[i].Instant >= tsx[i + 1].Instant, $"Transaction {i} should be more recent than {i + 1}");
    }

    [TestMethod]
    public async Task GetAllTransactions_FilteredBySecondUser()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        var now = DateTime.UtcNow;
        await CreateTestTransactions(now, svc);

        // Only transactions between user 0 and user 1
        var tsx = await svc.GetAllTransactions(0, 1);

        Assert.HasCount(3, tsx);
        // All involve user 1
        foreach (var t in tsx)
            Assert.IsTrue(t.FromId == 1 || t.ToId == 1);
    }

    [TestMethod]
    public async Task Transaction_UnitStoredAsLowercase()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        await svc.CreateTransaction(0, 1, 10, "GBP", null, DateTime.UtcNow);

        var tsx = (await svc.GetTransactions()).ToArray();

        Assert.HasCount(1, tsx);
        Assert.AreEqual("gbp", tsx[0].Unit);
    }

    [TestMethod]
    public async Task CreateTransaction_NegativeAmount_Throws()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => svc.CreateTransaction(0, 1, -10, "GBP", null, DateTime.UtcNow));
    }

    [TestMethod]
    public async Task CreateTransaction_SelfTransaction_Throws()
    {
        var svc = new DatabaseTransactions(new SqliteInMemoryDatabase());

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.CreateTransaction(0, 0, 10, "GBP", null, DateTime.UtcNow));
    }
}