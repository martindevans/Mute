using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    public class Iou
        : InteractiveBase
    {
        private readonly IouDatabaseService _database;

        public Iou(IouDatabaseService database)
        {
            _database = database;
        }

        [Command("iou"), Summary("I will remember that you owe something to another user")]
        public async Task CreateDebt(IUser user, decimal amount, string unit, [Remainder] string note = null)
        {
            if (amount < 0)
                await this.TypingReplyAsync("You cannot owe a negative amount!");

            using (Context.Channel.EnterTypingState())
            {
                await _database.Insert(user, Context.User, amount, unit, note);

                var symbol = unit.TryGetCurrencySymbol();
                if (unit == symbol)
                    await ReplyAsync($"{Context.User.Mention} owes {amount}{unit} to {user.Mention}");
                else
                    await ReplyAsync($"{Context.User.Mention} owes {symbol}{amount} to {user.Mention}");
            }
        }

        private static string FormatCurrency(decimal amount, string unit)
        {
            var sym = unit.TryGetCurrencySymbol();

            if (unit == sym)
                return $"{amount}({unit.ToUpperInvariant()})";
            else
                return $"{sym}{amount}";
        }

        private async Task SpeakResult(IReadOnlyList<Owed> owed, Func<string> nothing, Func<Owed, string> singleResult, Func<IReadOnlyList<Owed>, string> fewResults, Func<IReadOnlyList<Owed>, string> manyPrelude, Func<Owed, int, string> debtToString)
        {
            if (owed.Count == 0)
            {
                await this.TypingReplyAsync(nothing());
                return;
            }

            //Make sure we have a fresh user list to resolve users from IDs
            await Context.Guild.DownloadUsersAsync();

            if (owed.Count == 1)
            {
                await this.TypingReplyAsync(singleResult(owed.Single()));
            }
            else if (owed.Count < 5)
            {
                await this.TypingReplyAsync(fewResults(owed));
            }
            else
            {
                await this.TypingReplyAsync(manyPrelude(owed));

                var index = 0;
                foreach (var debt in owed)
                    await this.TypingReplyAsync(debtToString(debt, index++));
            }
        }

        [Command("io"), Summary("I will tell you what you currently owe")]
        public async Task ListDebtsByBorrower(IUser lender = null)
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

                await SpeakResult(owed, 
                    () => "You are debt free :D",
                    o => $"{Context.User.Mention} owes {Context.Client.GetUser(o.LenderId).Mention} {FormatCurrency(o.Amount, o.Unit)}",
                    os => $"{Context.User.Mention} owes {string.Join(", ", os.Select(FormatOwed))}",
                    os => $"{Context.User.Mention} owes {os.Count} debts...",
                    (o, i) => $"{i+1}. {FormatOwed(o)}"
                );
            }
        }

        [Command("oi"), Summary("I will tell you what you are currently owed")]
        public async Task ListDebtsByLender(IUser borrower = null)
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

                await SpeakResult(owed, 
                    () => "You are owed nothing",
                    o => $"{Context.Client.GetUser(o.BorrowerId).Mention} owes {Context.User.Mention} {FormatCurrency(o.Amount, o.Unit)}",
                    os => $"{Context.User.Mention} is owed {string.Join(", ", os.Select(FormatBorrowed))}",
                    os => $"{Context.User.Mention} is owed {os.Count} debts...",
                    (o, i) => $"{i+1}. {FormatBorrowed(o)}"
                );
            }
        }
    }
}
