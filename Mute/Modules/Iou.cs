using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    public class Iou
        : InteractiveBase
    {
        private readonly IouDatabaseService _database;
        private readonly Random _random;

        public Iou(IouDatabaseService database, Random random)
        {
            _database = database;
            _random = random;
        }

        #region debts
        [Command("iou"), Summary("I will remember that you owe something to another user")]
        public async Task CreateDebt([NotNull] IUser user, decimal amount, string unit, [CanBeNull, Remainder] string note = null)
        {
            if (amount < 0)
                await this.TypingReplyAsync("You cannot owe a negative amount!");

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
            string FormatOwed(Owed owe)
            {
                var lenderUser = Context.Client.GetUser(owe.LenderId);
                return $"{lenderUser.Mention} {FormatCurrency(owe.Amount, owe.Unit)}";
            }

            using (Context.Channel.EnterTypingState())
            {
                var owed = (await _database.GetOwed(Context.User))
                    .Where(o => lender == null || o.LenderId == lender.Id)
                    .OrderBy(o => o.LenderId)
                    .ToArray();

                await SpeakItems(owed, 
                    () => "You are debt free :D",
                    o => $"{Context.User.Mention} owes {Context.Client.GetUser(o.LenderId).Mention} {FormatCurrency(o.Amount, o.Unit)}",
                    os => $"{Context.User.Mention} owes {string.Join(", ", os.Select(FormatOwed))}",
                    os => $"{Context.User.Mention} owes {os.Count} debts...",
                    (o, i) => $"{i+1}. {FormatOwed(o)}"
                );
            }
        }

        [Command("oi"), Summary("I will tell you what you are currently owed")]
        public async Task ListDebtsByLender([CanBeNull] IUser borrower = null)
        {
            string FormatBorrowed(Owed owe)
            {
                var borrowerUser = Context.Client.GetUser(owe.BorrowerId);
                return $"{FormatCurrency(owe.Amount, owe.Unit)} by {borrowerUser.Mention}";
            }

            using (Context.Channel.EnterTypingState())
            {
                var owed = (await _database.GetLent(Context.User))
                    .Where(o => borrower == null || o.BorrowerId == borrower.Id)
                    .OrderBy(o => o.LenderId)
                    .ToArray();

                await SpeakItems(owed, 
                    () => "You are owed nothing",
                    o => $"{Context.Client.GetUser(o.BorrowerId).Mention} owes {Context.User.Mention} {FormatCurrency(o.Amount, o.Unit)}",
                    os => $"{Context.User.Mention} is owed {string.Join(", ", os.Select(FormatBorrowed))}",
                    os => $"{Context.User.Mention} is owed {os.Count} debts...",
                    (o, i) => $"{i+1}. {FormatBorrowed(o)}"
                );
            }
        }
        #endregion

        #region payments
        [Command("pay"), Summary("I will record that you have paid someone else some money")]
        public async Task CreatePendingPayment([NotNull] IUser receiver, decimal amount, string unit, [CanBeNull] [Remainder] string note = null)
        {
            if (amount < 0)
                await this.TypingReplyAsync("You cannot pay a negative amount!");

            using (Context.Channel.EnterTypingState())
            {
                var id = unchecked((uint)_random.Next()).MeaninglessString();

                await _database.InsertUnconfirmedPayment(Context.User, receiver, amount, unit, note, id);
                await this.TypingReplyAsync($"{receiver.Mention} type `!confirm {id}` to confirm that you have received this payment");
            }
        }

        [Command("confirm"), Summary("I will record that you have received the pending payment")]
        public async Task ConfirmPendingPayment(string id)
        {
            using (Context.Channel.EnterTypingState())
            {
                var result = await _database.ConfirmPending(id);

                if (result.HasValue)
                    await ReplyAsync($"{Context.User.Mention} Confirmed receipt of {FormatCurrency(result.Value.Amount, result.Value.Unit)} from {Context.Client.GetUser(result.Value.PayerId).Mention}");
                else
                    await ReplyAsync($"{Context.User.Mention} I can't find a pending payment with that ID");
            }
        }

        [Command("pending"), Summary("I will list all pending transactions you have yet to confirm")]
        public async Task ListPendingPayments()
        {
            string Note(Pending pending)
            {
                if (string.IsNullOrEmpty(pending.Note))
                    return "";
                else
                    return $" for `{pending.Note}`";
            }

            using (Context.Channel.EnterTypingState())
            {
                var pending = await _database.GetPending(Context.User);

                await SpeakItems(
                    pending,
                    () => "You have no pending payments to confirm",
                    p => $"Type `!confirm {p.Id}` to confirm receipt of {FormatCurrency(p.Amount, p.Unit)} from {Context.Client.GetUser(p.PayerId).Mention}{Note(p)}",
                    null,
                    ps => $"You have {ps.Count} payments to confirm. Type `!confirm $id` for each payment you have received",
                    (p, i) => $"{p.Id}: {Context.Client.GetUser(p.PayerId).Mention} paid you {FormatCurrency(p.Amount, p.Unit)}, '{p.Note}'"
                );
            }
        }
        #endregion

        #region helpers
        /// <summary>
        /// Generate a human readable string to represent the given amount/currency pair
        /// </summary>
        /// <param name="amount"></param>
        /// <param name="unit"></param>
        /// <returns></returns>
        [NotNull] private static string FormatCurrency(decimal amount, string unit)
        {
            var sym = unit.TryGetCurrencySymbol();

            if (unit == sym)
                return $"{amount}({unit.ToUpperInvariant()})";
            else
                return $"{sym}{amount}";
        }

        /// <summary>
        /// Speak a list of items. Will use different formats for none, few and many items.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="items">The list of items to speak</param>
        /// <param name="nothing">Generate a string for no items</param>
        /// <param name="singleResult">Generate a string for a single item</param>
        /// <param name="fewResults">Generate a string for the given set of results</param>
        /// <param name="manyPrelude">Generate a string to say before speaking many results</param>
        /// <param name="itemToString">Convert a single item (of many) to a string</param>
        /// <returns></returns>
        private async Task SpeakItems<T>([NotNull] IReadOnlyList<T> items, Func<string> nothing, Func<T, string> singleResult, Func<IReadOnlyList<T>, string> fewResults, Func<IReadOnlyList<T>, string> manyPrelude, Func<T, int, string> itemToString)
        {
            if (items.Count == 0)
            {
                await this.TypingReplyAsync(nothing());
                return;
            }

            //Make sure we have a fresh user list to resolve users from IDs
            await Context.Guild.DownloadUsersAsync();

            if (items.Count == 1)
            {
                await this.TypingReplyAsync(singleResult(items.Single()));
            }
            else if (items.Count < 5 && fewResults != null)
            {
                await this.TypingReplyAsync(fewResults(items));
            }
            else
            {
                await this.TypingReplyAsync(manyPrelude(items));

                var index = 0;
                foreach (var debt in items)
                    await this.TypingReplyAsync(itemToString(debt, index++));
            }
        }
        #endregion
    }
}
