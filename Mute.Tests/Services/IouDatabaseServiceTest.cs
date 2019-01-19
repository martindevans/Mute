using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Discord.Services;
using Mute.Moe.Services.Database;

namespace Mute.Tests.Services
{
    [TestClass]
    public class IouDatabaseServiceTest
    {
        private readonly IouDatabaseService _db = new IouDatabaseService(new SqliteInMemoryDatabase());

        [TestMethod]
        public async Task InsertDebt_CreatesDebt()
        {
            await _db.InsertDebt(0, 1, 10, "GBP", "Note");

            var owed0 = (await _db.GetOwed(0)).ToArray();
            Assert.AreEqual(0, owed0.Length);

            var owed1 = (await _db.GetOwed(1)).ToArray();
            Assert.AreEqual(1, owed1.Length);
            var owedSingle = owed1.Single();

            Assert.AreEqual(10, owedSingle.Amount);
            Assert.AreEqual(1, (int)owedSingle.BorrowerId);
            Assert.AreEqual(0, (int)owedSingle.LenderId);
            Assert.AreEqual("gbp", owedSingle.Unit);
        }

        [TestMethod]
        public async Task InsertMultipleDebts_BalancesTotal()
        {
            //10+10=20
            await _db.InsertDebt(0, 1, 10, "GBP", "A");
            await _db.InsertDebt(0, 1, 10, "GBP", "A");

            //5+10=15
            await _db.InsertDebt(1, 0, 5, "GBP", "B");
            await _db.InsertDebt(1, 0, 10, "GBP", "B");

            var owed0 = (await _db.GetOwed(0)).ToArray();
            Assert.AreEqual(0, owed0.Length);

            var owed1 = (await _db.GetOwed(1)).ToArray();
            Assert.AreEqual(1, owed1.Length);
            var owedSingle = owed1.Single();

            //Total owed will be 20-15=5
            Assert.AreEqual(5, owedSingle.Amount);

            Assert.AreEqual(1, (int)owedSingle.BorrowerId);
            Assert.AreEqual(0, (int)owedSingle.LenderId);
            Assert.AreEqual("gbp", owedSingle.Unit);
        }

        [TestMethod]
        public async Task Payment_GetPending()
        {
            await _db.InsertUnconfirmedPayment(0, 1, 10, "gbp", "note", "guid");
            await _db.InsertUnconfirmedPayment(2, 1, 11, "usd", "note 2", "guid 2");

            //Get all pending payments
            var pending = (await _db.GetPendingForReceiver(1)).ToArray();

            //Get the two payments
            Assert.AreEqual(2, pending.Length);
            var usd = pending.Single(a => a.Unit == "usd");
            var gbp = pending.Single(a => a.Unit == "gbp");

            Assert.AreEqual(11, (int)usd.Amount);
            Assert.AreEqual("note 2", usd.Note);
            Assert.AreEqual(2, (int)usd.PayerId);

            Assert.AreEqual(10, (int)gbp.Amount);
            Assert.AreEqual("note", gbp.Note);
            Assert.AreEqual(0, (int)gbp.PayerId);
        }

        [TestMethod]
        public async Task Payment_CreateDebt()
        {
            //Create a payment of 10GBP from 0 -> 1
            await _db.InsertUnconfirmedPayment(0, 1, 10, "gbp", "note", "guid");

            //Confirm that payment
            var confirmed = await _db.ConfirmPending("guid", 1);
            Assert.IsTrue(confirmed.HasValue);
            Assert.AreEqual(10, (int)confirmed.Value.Amount);
            Assert.AreEqual(0, (int)confirmed.Value.PayerId);
            Assert.AreEqual(1, (int)confirmed.Value.ReceiverId);

            //Now check that the balance between 0 and 1 is correct (1 owes 10GBP to 0)
            var owed1 = (await _db.GetOwed(1)).ToArray();
            Assert.AreEqual(1, owed1.Length);
            var owedSingle = owed1.Single();

            Assert.AreEqual(10, owedSingle.Amount);
            Assert.AreEqual(1, (int)owedSingle.BorrowerId);
            Assert.AreEqual(0, (int)owedSingle.LenderId);
            Assert.AreEqual("gbp", owedSingle.Unit);
        }

        [TestMethod]
        public async Task Payment_CannotBeConfirmedByThirdParty()
        {
            //Create a payment of 10GBP from 0 -> 1
            await _db.InsertUnconfirmedPayment(0, 1, 10, "gbp", "note", "guid");

            //Try to confirm it as someone else
            Assert.IsNull(await _db.ConfirmPending("guid", 0));
            Assert.IsNull(await _db.ConfirmPending("guid", 2));

            //Confirm it as the person who received it
            Assert.IsNotNull(await _db.ConfirmPending("guid", 1));
        }

        [TestMethod]
        public async Task Payment_CannotBeDeniedByThirdParty()
        {
            //Create a payment of 10GBP from 0 -> 1
            await _db.InsertUnconfirmedPayment(0, 1, 10, "gbp", "note", "guid");

            //Try to confirm it as someone else
            Assert.IsNull(await _db.DenyPending("guid", 0));
            Assert.IsNull(await _db.DenyPending("guid", 2));

            //Confirm it as the person who received it
            Assert.IsNotNull(await _db.ConfirmPending("guid", 1));
        }

        [TestMethod]
        public async Task Payment_DenyPending_RemovesFromPendingList()
        {
            //Create a payment of 10GBP from 0 -> 1
            await _db.InsertUnconfirmedPayment(0, 1, 10, "gbp", "note", "guid");

            //Get pending before deny
            var pend0 = (await _db.GetPendingForReceiver(1)).ToArray();
            Assert.AreEqual(1, pend0.Length);
            Assert.AreEqual("guid", pend0[0].Id);

            //Check balance
            var bal0 = (await _db.GetLent(0)).ToArray();
            Assert.AreEqual(0, bal0.Length);

            //Deny it
            var denied = await _db.DenyPending("guid", 1);
            Assert.IsNotNull(denied);

            //Get pending after deny
            var pend1 = (await _db.GetPendingForReceiver(1)).ToArray();
            Assert.AreEqual(0, pend1.Length);

            //Check balance is unchanged
            var bal1 = (await _db.GetLent(0)).ToArray();
            Assert.AreEqual(0, bal1.Length);
        }
    }
}
