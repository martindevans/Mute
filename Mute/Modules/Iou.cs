using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.Recognizers.Text;
using Microsoft.Recognizers.Text.NumberWithUnit;
using MoreLinq;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    public class Iou
        : BaseModule
    {
        private readonly IouDatabaseService _database;
        private readonly Random _random;
        private readonly DiscordSocketClient _client;

        public Iou(IouDatabaseService database, Random random, DiscordSocketClient client)
        {
            _database = database;
            _random = random;
            _client = client;
        }

        #region debts
        [Command("iou"), Summary("I will remember that you owe something to another user")]
        public async Task CreateDebt([NotNull] IUser user, decimal amount, [NotNull] string unit, [CanBeNull, Remainder] string note = null)
        {
            if (amount < 0)
                await TypingReplyAsync("You cannot owe a negative amount!");

            await CheckDebugger();

            using (Context.Channel.EnterTypingState())
            {
                await _database.InsertDebt(user, Context.User, amount, unit, note);

                var symbol = unit.TryGetCurrencySymbol();
                if (unit == symbol)
                    await ReplyAsync($"{Context.User.Mention} owes {amount}{unit} to {user.Mention}");
                else
                    await ReplyAsync($"{Context.User.Mention} owes {symbol}{amount} to {user.Mention}");
            }
        }

        [Command("io"), Summary("I will tell you what you currently owe")]
        public async Task ListDebtsByBorrower([CanBeNull] IUser lender = null)
        {
            await CheckDebugger();

            using (Context.Channel.EnterTypingState())
            {
                var owed = (await _database.GetOwed(Context.User))
                    .Where(o => lender == null || o.LenderId == lender.Id)
                    .OrderBy(o => o.LenderId)
                    .ToArray();

                await PaginatedTransactions(owed, "owes", false);
            }
        }

        [Command("oi"), Summary("I will tell you what you are currently owed")]
        public async Task ListDebtsByLender([CanBeNull] IUser borrower = null)
        {
            await CheckDebugger();

            using (Context.Channel.EnterTypingState())
            {
                var owed = (await _database.GetLent(Context.User))
                    .Where(o => borrower == null || o.BorrowerId == borrower.Id)
                    .OrderBy(o => o.LenderId)
                    .ToArray();

                await PaginatedTransactions(owed, "owes", false);
            }
        }
        #endregion

        #region payments/demands
        [Command("uoi"), Summary("I will notify someone that they owe you money")]
        public async Task CreateDebtDemand([NotNull] IUser debter, decimal amount, [NotNull] string unit, [CanBeNull] [Remainder] string note = null)
        {
            if (amount < 0)
                await TypingReplyAsync("You cannot demand a negative amount!");

            await CheckDebugger();

            using (Context.Channel.EnterTypingState())
            {
                var id = unchecked((uint)_random.Next()).MeaninglessString();

                await _database.InsertUnconfirmedPayment(Context.User, debter, amount, unit, note, id);
                await TypingReplyAsync($"{debter.Mention} type `!confirm {id}` to confirm that you owe this");
            }
        }
        
        [Command("pay"), Summary("I will record that you have paid someone else some money")]
        public async Task CreatePendingPayment([NotNull] IUser receiver, decimal amount, [NotNull] string unit, [CanBeNull] [Remainder] string note = null)
        {
            if (amount < 0)
                await TypingReplyAsync("You cannot pay a negative amount!");

            await CheckDebugger();

            using (Context.Channel.EnterTypingState())
            {
                var id = unchecked((uint)_random.Next()).MeaninglessString();

                await _database.InsertUnconfirmedPayment(Context.User, receiver, amount, unit, note, id);
                await TypingReplyAsync($"{receiver.Mention} type `!confirm {id}` to confirm that you have received this payment");
            }
        }

        [Command("confirm"), Summary("I will record that you confirm the pending transaction")]
        public async Task ConfirmPendingPayment(string id)
        {
            await CheckDebugger();

            using (Context.Channel.EnterTypingState())
            {
                var result = await _database.ConfirmPending(id);

                if (result.HasValue)
                    await ReplyAsync($"{Context.User.Mention} Confirmed transaction of {FormatCurrency(result.Value.Amount, result.Value.Unit)} from {Context.Client.GetUser(result.Value.PayerId).Mention} to {Context.Client.GetUser(result.Value.ReceiverId).Mention}");
                else
                    await ReplyAsync($"{Context.User.Mention} I can't find a pending payment with that ID");
            }
        }

        [Command("pending"), Summary("I will list all pending transactions you have yet to confirm")]
        public async Task ListPendingPayments()
        {
            await CheckDebugger();

            using (Context.Channel.EnterTypingState())
                await PaginatedPending(await _database.GetPending(Context.User));
        }
        #endregion

        #region transaction list
        [Command("transactions"), Summary("I will show all your transactions, optionally filtered to only with another user")]
        public async Task ListTransactions([CanBeNull] IUser other = null)
        {
            await CheckDebugger();

            var tsx = other == null
                ? _database.GetTransactions(Context.Message.Author.Id)
                : _database.GetTransactions(Context.Message.Author.Id, other.Id);

            await PaginatedTransactions(await tsx);
        }
        #endregion

        #region helpers
        private async Task CheckDebugger()
        {
            if (Debugger.IsAttached)
                await ReplyAsync("**Warning - Debugger is attached. This is likely not the main version of mute!**");
        }

        private string Name(ulong id)
        {
            var user = _client.GetUser(id);

            if (user == null)
                return $"?{id}?";

            if (user is IGuildUser gu)
                return gu.Nickname;

            return user.Username;
        }

        private async Task PaginatedTransactions([NotNull] IEnumerable<Owed> owed, string connector = "➜", bool lenderFirst = true)
        {
            string FormatSingleTsx(Owed d)
            {
                var borrower = Name(d.BorrowerId);
                var lender = Name(d.LenderId);
                var note = string.IsNullOrEmpty(d.Note) ? "" : $"'{d.Note}'";

                return $"{(lenderFirst ? lender : borrower)} {connector} {(lenderFirst ? borrower : lender)} {FormatCurrency(d.Amount, d.Unit)} {note}";
            }

            var owedArr = owed.ToArray();

            //If the number of transactions is small, display them all.
            //Otherwise batch and show them in pages
            if (owedArr.Length < 10)
                await ReplyAsync(string.Join("\n", owedArr.Select(FormatSingleTsx)));
            else
                await PagedReplyAsync(new PaginatedMessage {Pages = owedArr.Batch(7).Select(d => string.Join("\n", d.Select(FormatSingleTsx)))});
        }

        private async Task PaginatedPending([NotNull] IEnumerable<Pending> pending)
        {
            string FormatSinglePending(Pending p, bool longForm)
            {
                var payer = Name(p.PayerId);
                var note = string.IsNullOrEmpty(p.Note) ? "" : $"'{p.Note}'";
                var amount = FormatCurrency(p.Amount, p.Unit);

                if (longForm)
                    return $"Type `!confirm {p.Id}` to confirm transaction of {amount} from {payer} {note}";
                else
                    return $"`{p.Id}`: {payer} paid you {amount} {note}";
            }

            var pendingArr = pending.ToArray();

            //If the number of transactions is small, display them all.
            //Otherwise batch and show them in pages
            if (pendingArr.Length < 7)
                await ReplyAsync(string.Join("\n", pendingArr.Select(p => FormatSinglePending(p, true))));
            else
            {
                await TypingReplyAsync($"You have {pendingArr.Length} payments to confirm. Type `!confirm $id` for each payment you have received");
                await PagedReplyAsync(new PaginatedMessage {Pages = pendingArr.Batch(5).Select(d => string.Join("\n", d.Select(p => FormatSinglePending(p, false))))});
            }
        }

        /// <summary>
        /// Generate a human readable string to represent the given amount/currency pair
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        [NotNull] private static string FormatCurrency(decimal amount, [NotNull] string unit)
        {
            var sym = unit.TryGetCurrencySymbol();

            if (unit == sym)
                return $"{amount}({unit.ToUpperInvariant()})";
            else
                return $"{sym}{amount}";
        }

        [NotNull] private static Extraction ExtractCurrencyAndAmount(string input)
        {
            Extraction Extract()
            {
                var results = NumberWithUnitRecognizer.RecognizeCurrency(input, Culture.EnglishOthers);

                //Try to get the result
                var cur = results.FirstOrDefault(d => d.TypeName.StartsWith("currency"));
                if (cur == null)
                    return null;

                var values = cur.Resolution;

                if (!values.TryGetValue("unit", out var unitObj) || !(unitObj is string unit))
                    return new Extraction("unit");

                if (!values.TryGetValue("value", out var valueObj) || !(valueObj is string value))
                    return new Extraction("value");

                if (!decimal.TryParse(value, out var deci))
                    return new Extraction("value");

                return new Extraction(unit, deci);
            }

            return Extract() ?? new Extraction("value,unit");
        }

        private class Extraction
        {
            public bool IsValid { get; }

            public string Currency { get; }
            public decimal Amount { get; }

            public string ErrorMessage { get; }

            public Extraction(string error)
            {
                IsValid = false;
                ErrorMessage = error;

                Currency = null;
                Amount = 0;
            }

            public Extraction(string currency, decimal amount)
            {
                IsValid = true;
                Currency = currency;
                Amount = amount;

                ErrorMessage = null;
            }
        }
        #endregion
    }
}
