﻿using System;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Payment;

namespace Mute.Tests.Services.Payments
{
    [TestClass]
    public class DatabaseTransactionServiceTests
    {
        [TestMethod]
        public async Task CreateTransactionDoesNotThrow()
        {
            var svc = new DatabaseTransactionService(new SqliteInMemoryDatabase());

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

        [TestMethod]
        public async Task GetTransactionByFromId()
        {
            var svc = new DatabaseTransactionService(new SqliteInMemoryDatabase());

            var now = DateTime.UtcNow;
            await CreateTestTransactions(now, svc);

            var tsx = await (await svc.GetTransactions(fromId: 0)).OrderBy(a => a.Instant).ToArray();

            Assert.AreEqual(2, tsx.Length);
            Assert.AreEqual("test", tsx[0].Unit);
            Assert.AreEqual("test2", tsx[1].Unit);
        }

        [TestMethod]
        public async Task GetTransactionByToId()
        {
            var svc = new DatabaseTransactionService(new SqliteInMemoryDatabase());

            var now = DateTime.UtcNow;
            await CreateTestTransactions(now, svc);

            var tsx = await (await svc.GetTransactions(toId: 0)).OrderBy(a => a.Instant).ToArray();

            Assert.AreEqual(1, tsx.Length);
            Assert.AreEqual("test", tsx[0].Unit);
        }

        [TestMethod]
        public async Task GetTransactionByUnit()
        {
            var svc = new DatabaseTransactionService(new SqliteInMemoryDatabase());

            var now = DateTime.UtcNow;
            await CreateTestTransactions(now, svc);

            var tsx = await (await svc.GetTransactions(unit: "test")).OrderBy(a => a.Instant).ToArray();

            Assert.AreEqual(2, tsx.Length);
            Assert.AreEqual("test", tsx[0].Unit);
            Assert.AreEqual("test", tsx[1].Unit);
        }

        [TestMethod]
        public async Task GetTransactionByBefore()
        {
            var svc = new DatabaseTransactionService(new SqliteInMemoryDatabase());

            var now = DateTime.UtcNow;
            await CreateTestTransactions(now, svc);

            var tsx = await (await svc.GetTransactions(before: now + TimeSpan.FromMinutes(2.5))).OrderBy(a => a.Instant).ToArray();

            Assert.AreEqual(2, tsx.Length);
            Assert.AreEqual("test", tsx[0].Unit);
            Assert.AreEqual("test", tsx[1].Unit);
        }

        [TestMethod]
        public async Task GetTransactionByAfter()
        {
            var svc = new DatabaseTransactionService(new SqliteInMemoryDatabase());

            var now = DateTime.UtcNow;
            await CreateTestTransactions(now, svc);

            var tsx = await (await svc.GetTransactions(after: now + TimeSpan.FromMinutes(2.5))).OrderBy(a => a.Instant).ToArray();

            Assert.AreEqual(2, tsx.Length);
            Assert.AreEqual("test2", tsx[0].Unit);
            Assert.AreEqual("test3", tsx[1].Unit);
        }

        [TestMethod]
        public async Task GetBalances01()
        {
            var svc = new DatabaseTransactionService(new SqliteInMemoryDatabase());

            var now = DateTime.UtcNow;
            await CreateTestTransactions(now, svc);

            var balances = (await svc.GetBalances(0, 1)).ToArray();

            Assert.AreEqual(2, balances.Length);
        }

        [TestMethod]
        public async Task GetBalances10()
        {
            var svc = new DatabaseTransactionService(new SqliteInMemoryDatabase());

            var now = DateTime.UtcNow;
            await CreateTestTransactions(now, svc);

            var balances = (await svc.GetBalances(1, 0)).ToArray();

            Assert.AreEqual(2, balances.Length);
        }

        [TestMethod]
        public async Task GetBalances19()
        {
            var svc = new DatabaseTransactionService(new SqliteInMemoryDatabase());

            var now = DateTime.UtcNow;
            await CreateTestTransactions(now, svc);

            var balances = (await svc.GetBalances(1, null)).ToArray();

            Assert.AreEqual(3, balances.Length);

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
    }
}