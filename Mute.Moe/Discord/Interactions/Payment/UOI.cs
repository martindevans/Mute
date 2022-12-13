using System;
using System.Linq;
using System.Threading.Tasks;
using BalderHash;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using JetBrains.Annotations;
using Mute.Moe.Discord.Services.Users;
using Mute.Moe.Services.Payment;

namespace Mute.Moe.Discord.Interactions.Payment;

[UsedImplicitly]
public class UOI
    : BaseInteractionModule
{
    private readonly IPendingTransactions _pending;
    private readonly IUserService _users;

    public UOI(IPendingTransactions pending, IUserService users)
    {
        _pending = pending;
        _users = users;
    }

    [SlashCommand("uoi", "I will I will notify someone that they owe you something")]
    public async Task CreatePendingUoi(IUser user, decimal amount, string unit, [Remainder] string? note = null)
    {
        if (amount < 0)
        {
            await RespondAsync("You cannot owe a negative amount!");
        }
        else
        {
            var id = await _pending.CreatePending(Context.User.Id, user.Id, amount, unit, note, DateTime.UtcNow);
            var fid = new BalderHash32(id).ToString();

            await RespondAsync($"{user.Mention} type `/confirm {fid}` to confirm that you owe this");
            await FollowupAsync($"{user.Mention} type `/deny {fid}` to deny that you owe this. Please talk to the other user about why!");
        }
    }

    [SlashCommand("pay", "I will record that you have paid someone something")]
    public async Task CreatePendingPayment(IUser user, decimal amount, string unit, [Remainder] string? note = null)
    {
        if (amount < 0)
        {
            await RespondAsync("You cannot pay a negative amount!");
        }
        else
        {
            var id = await _pending.CreatePending(Context.User.Id, user.Id, amount, unit, note, DateTime.UtcNow);
            var fid = new BalderHash32(id).ToString();

            await RespondAsync($"{user.Mention} type `/confirm {fid}` to confirm that you have been paid this");
            await FollowupAsync($"{user.Mention} type `/deny {fid}` to deny that you received this payment. Please talk to the other user about why!");
        }
    }

    [SlashCommand("confirm", "I will confirm a pending transaction")]
    public async Task Confirm(string input)
    {
        var fid = BalderHash32.Parse(input);
        if (!fid.HasValue)
        {
            await RespondAsync("Invalid ID `{id}`");
            return;
        }

        var transactions = await _pending.Get(debtId: fid.Value.Value).ToListAsync();
        if (transactions.Count == 0)
        {
            await RespondAsync($"Cannot find a transaction with ID `{fid}`");
            return;
        }

        if (transactions.Count > 1)
        {
            await RespondAsync($"Found multiple transactions with ID `{fid}`! This is probably an error, please report it.");
            return;
        }

        var transaction = transactions[0];
        if (transaction.ToId != Context.User.Id)
        {
            await RespondAsync("You cannot confirm a transaction which does not belong to you");
            return;
        }

        var result = await _pending.ConfirmPending(fid.Value.Value);
        switch (result)
        {
            case ConfirmResult.Confirmed:
                await RespondAsync($"Confirmed {await transaction.Format(_users)}");
                break;
            case ConfirmResult.AlreadyDenied:
                await RespondAsync("This transaction has already been denied and cannot be confirmed");
                break;
            case ConfirmResult.AlreadyConfirmed:
                await RespondAsync("This transaction has already been confirmed");
                break;
            case ConfirmResult.IdNotFound:
                await RespondAsync($"Cannot find a transaction with ID `{fid}`! This is probably an error, please report it.");
                break;
            default:
                await RespondAsync($"Unknown transaction state `{result}`! This is probably an error, please report it.");
                throw new ArgumentOutOfRangeException();
        }
    }

    [SlashCommand("deny", "I will deny a pending transaction")]
    public async Task Deny(string input)
    {
        var fid = BalderHash32.Parse(input);
        if (!fid.HasValue)
        {
            await RespondAsync($"Invalid ID `{fid}`");
            return;
        }

        var transactions = await _pending.Get(debtId: fid.Value.Value).ToListAsync();
        if (transactions.Count == 0)
        {
            await RespondAsync($"Cannot find a transaction with ID `{fid}`");
            return;
        }

        if (transactions.Count > 1)
        {
            await RespondAsync($"Found multiple transactions with ID `{fid}`! This is probably an error, please report it.");
            return;
        }

        var transaction = transactions[0];
        if (transaction.ToId != Context.User.Id)
        {
            await RespondAsync("You cannot deny a transaction which does not belong to you");
            return;
        }

        var result = await _pending.DenyPending(fid.Value.Value);
        switch (result)
        {
            case DenyResult.Denied:
                await RespondAsync($"Denied {await transaction.Format(_users, true)}");
                break;
            case DenyResult.AlreadyDenied:
                await RespondAsync("This transaction has already been denied");
                break;
            case DenyResult.AlreadyConfirmed:
                await RespondAsync("This transaction has already been confirmed and can no longer be denied");
                break;
            case DenyResult.IdNotFound:
                await RespondAsync($"Cannot find a transaction with ID `{fid}`! This is probably an error, please report it.");
                break;
            default:
                await RespondAsync($"Unknown transaction state `{result}`! This is probably an error, please report it.");
                throw new ArgumentOutOfRangeException();
        }
    }

    //[SlashCommand("pending", "I will list pending transactions you have yet to confirm")]
    //public async Task Pending()
    //{
    //    await PaginatedPending(
    //        _pending.Get(toId: Context.User.Id, state: PendingState.Pending),
    //        "No pending transactions to confirm",
    //        "You have {0} payments to confirm. Type `!confirm $id` to confirm that it has happened or `!deny $id` otherwise",
    //        mentionReceiver: false
    //    );
    //}

    //[SlashCommand("pending-in", "I will list pending transactions involving you which the other person has no yet confirmed")]
    //public async Task ReversePending()
    //{
    //    await PaginatedPending(
    //        _pending.Get(fromId: Context.User.Id, state: PendingState.Pending),
    //        "No pending transactions involving you for others to confirm",
    //        "There are {0} unconfirmed payments to you. The other person should type `!confirm $id` or `!deny $id` to confirm or deny that the payment has happened",
    //        mentionReceiver: true
    //    );
    //}

    //private async Task PaginatedPending(IAsyncEnumerable<IPendingTransaction> pending, string none, string paginatedHeader, bool mentionReceiver)
    //{
    //    async Task<string> FormatSinglePending(IPendingTransaction p, bool longForm)
    //    {
    //        var receiver = await _users.Name(p.ToId, mention: mentionReceiver);
    //        var payer = await _users.Name(p.FromId);
    //        var note = string.IsNullOrEmpty(p.Note) ? "" : $"'{p.Note}'";
    //        var amount = TransactionFormatting.FormatCurrency(p.Amount, p.Unit);

    //        var fid = new BalderHash32(p.Id).ToString();
    //        if (longForm)
    //            return $"{receiver} Type `!confirm {fid}` or `!deny {fid}` to confirm/deny transaction of {amount} from {payer} {note}";
    //        else
    //            return $"`{fid}`: {payer} paid {amount} to {receiver} {note}";
    //    }

    //    var pendingArr = await pending.ToListAsync();
    //    var formatted = new List<string>();
    //    var longForm = pendingArr.Count < 5;
    //    foreach (var item in pendingArr)
    //        formatted.Add(await FormatSinglePending(item, longForm));

    //    if (pendingArr.Count == 0)
    //        await RespondAsync(none);
    //    else if (longForm)
    //        await RespondAsync(string.Join("\n", formatted));
    //    else
    //    {
    //        await RespondAsync(string.Format(paginatedHeader, pendingArr.Count));
    //        await PagedReplyAsync(new PaginatedMessage { Pages = formatted.Batch(7).Select(d => string.Join("\n", d)) });
    //    }
    //}
}