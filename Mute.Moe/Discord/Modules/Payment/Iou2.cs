using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using MoreLinq;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Payment;

namespace Mute.Moe.Discord.Modules.Payment
{
    [Group("iou2")]
    [RequireOwner]
    [WarnDebugger]
    [TypingReply]
    public class Iou2
        : BaseModule
    {
        private readonly ITransactions _transactions;
        private readonly DiscordSocketClient _client;

        public Iou2(ITransactions transactions, DiscordSocketClient client)
        {
            _transactions = transactions;
            _client = client;

            //todo:
            // uoi
            // pay
            // confirm
            // deny
            // pending
        }

        #region helpers
        private string Name(ulong id, bool mention = false)
        {
            var user = _client.GetUser(id);
            if (user == null)
                return $"UNKNOWN_USER:{id}";

            return Name(user);
        }

        private static string Name([NotNull] IUser user, bool mention = false)
        {
            if (mention)
                return user.Mention;

            return (user as IGuildUser)?.Nickname ?? user.Username;
        }

        [NotNull] private static string FormatCurrency(decimal amount, [NotNull] string unit)
        {
            var sym = unit.TryGetCurrencySymbol();

            if (unit == sym)
                return $"{amount}({unit.ToUpperInvariant()})";
            else
                return $"{sym}{amount}";
        }

        [NotNull] private string FormatTransaction([NotNull] ITransaction transaction)
        {
            var from = Name(transaction.FromId);
            var to = Name(transaction.ToId);
            var note = string.IsNullOrWhiteSpace(transaction.Note) ? "" : $"'{transaction.Note}'";

            return $"[{transaction.Instant:HH\\:mm UTC dd-MMM-yyyy}] {FormatCurrency(transaction.Amount, transaction.Unit)} {from} => {to} {note}";
        }

        [NotNull] private string FormatBalance([NotNull] IBalance balance)
        {
            var a = Name(balance.UserA);
            var b = Name(balance.UserB);

            var (borrower, lender) = balance.Amount < 0 ? (a, b) : (b, a);

            return $"{borrower} owes {FormatCurrency(Math.Abs(balance.Amount), balance.Unit)} to {lender}";
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
            //If the number of balances is small, display them all.
            //Otherwise batch and show them in pages
            if (balances.Count < 13)
                await ReplyAsync(string.Join("\n", balances.Select(FormatBalance)));
            else
                await PagedReplyAsync(new PaginatedMessage { Pages = balances.Batch(10).Select(d => string.Join("\n", d.Select(FormatBalance))) });
        }
        #endregion

        [Command("iou"), Summary("I will remember that you owe something to another user")]
        public async Task CreateDebt([NotNull] IUser user, decimal amount, [NotNull] string unit, [CanBeNull, Remainder] string note = null)
        {
            if (amount < 0)
                await TypingReplyAsync("You cannot owe a negative amount!");

            await _transactions.CreateTransaction(user.Id, Context.User.Id, amount, unit, note, DateTime.UtcNow);
            await ReplyAsync($"{Context.User.Mention} owes {FormatCurrency(amount, unit)} to {user.Mention}");
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
