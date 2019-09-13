using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using JetBrains.Annotations;
using MoreLinq;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Payment;

namespace Mute.Moe.Discord.Modules.Payment
{
    [HelpGroup("payment")]
    [WarnDebugger]
    [TypingReply]
    [Summary("Record and query how much you owe people")]
    public class Iou
        : BaseModule
    {
        private readonly ITransactions _transactions;

        public Iou(ITransactions transactions)
        {
            _transactions = transactions;
        }

        #region helpers
        [NotNull] private string FormatTransaction([NotNull] ITransaction tsx)
        {
            return TransactionFormatting.FormatTransaction(this, tsx);
        }

        [NotNull] private string FormatBalance([NotNull] IBalance bal)
        {
            return TransactionFormatting.FormatBalance(this, bal);
        }

        private async Task DisplayTransactions([NotNull] IReadOnlyCollection<ITransaction> transactions)
        {
            //If the number of transactions is small, display them all.
            //Otherwise batch and show them in pages
            if (transactions.Count < 13)
                await ReplyAsync(string.Join("\n", transactions.Select(FormatTransaction)));
            else
                await PagedReplyAsync(new PaginatedMessage { Pages = transactions.Batch(10).Select(d => string.Join("\n", d.Select(FormatTransaction))) });
        }

        private async Task DisplayBalances([NotNull] IReadOnlyCollection<IBalance> balances)
        {
            async Task DebtTotalsPerUnit()
            {
                if (balances.Count > 1)
                {
                    var totals = balances.GroupBy(a => a.Unit)
                                         .Select(a => (a.Key, a.Sum(o => o.Amount)))
                                         .OrderByDescending(a => a.Item2)
                                         .ToArray();

                    var r = new StringBuilder("```\nTotals:\n");
                    foreach (var (key, amount) in totals)
                        r.AppendLine($" => {TransactionFormatting.FormatCurrency(amount, key)}");
                    r.AppendLine("```");

                    await ReplyAsync(r.ToString());
                }
            }

            var balancesArr = balances.ToArray();

            //If the number of transactions is small, display them all.
            //Otherwise batch and show them in pages
            if (balancesArr.Length < 10)
                await ReplyAsync(string.Join("\n", balancesArr.Select(FormatBalance)));
            else
                await PagedReplyAsync(new PaginatedMessage { Pages = balancesArr.Batch(7).Select(d => string.Join("\n", d.Select(FormatBalance))) });

            await DebtTotalsPerUnit();
        }
        #endregion

        [Command("iou"), Summary("I will remember that you owe something to another user")]
        public async Task CreateDebt([NotNull] IUser user, decimal amount, [NotNull] string unit, [CanBeNull, Remainder] string note = null)
        {
            if (amount < 0)
                await TypingReplyAsync("You cannot owe a negative amount!");

            await _transactions.CreateTransaction(user.Id, Context.User.Id, amount, unit, note, DateTime.UtcNow);
            await ReplyAsync($"{Context.User.Mention} owes {TransactionFormatting.FormatCurrency(amount, unit)} to {user.Mention}");
        }

        [Command("transactions"), Summary("I will show all your transactions")]
        public async Task ListTransactions([CanBeNull, Summary("Filter only to transactions with this user")] IUser other = null)
        {
            //Get all transactions in both directions
            var all = (await _transactions.GetAllTransactions(Context.User.Id, other?.Id)).ToArray();
            if (all.Length == 0)
                await ReplyAsync("No transactions");
            else
                await DisplayTransactions(all);
        }

        #region balance query
        [Command("io"), Summary("I will tell you what you owe")]
        public async Task ListDebtsByBorrower([CanBeNull, Summary("Filter debts by this lender")] IUser lender = null)
        {
            await ShowBalances(lender, b => b.UserA == Context.User.Id ^ b.Amount > 0, "You are debt free");
        }

        [Command("oi"), Summary("I will tell you what you are owed")]
        public async Task ListDebtsByLender([CanBeNull, Summary("Filter debts by this borrower")] IUser borrower = null)
        {
            await ShowBalances(borrower, b => b.UserB == Context.User.Id ^ b.Amount > 0, "Nobody owes you anything");
        }

        [Command("balance"), Summary("I will tell you your balance")]
        public async Task ShowBalance([CanBeNull, Summary("Filter only to transactions with this user")] IUser other = null)
        {
            await ShowBalances(other, _ => true, "No non-zero balances");
        }

        private async Task ShowBalances([CanBeNull] IUser other, [NotNull] Func<IBalance, bool> filter, string none)
        {
            var balances = (await _transactions.GetBalances(Context.User.Id, other?.Id)).Where(filter).ToArray();
            if (balances.Length == 0)
                await ReplyAsync(none);
            else
                await DisplayBalances(balances);
        }
        #endregion
    }
}
