using System.Threading.Tasks;
using BalderHash;
using Discord;
using Discord.Addons.Interactive;
using Discord.Commands;
using JetBrains.Annotations;
using MoreLinq;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Users;
using Mute.Moe.Services.Payment;

namespace Mute.Moe.Discord.Modules.Payment;

[UsedImplicitly]
[HelpGroup("payment")]
[WarnDebugger]
[TypingReply]
public class Uoi
    : BaseModule
{
    private readonly IPendingTransactions _pending;
    private readonly IUserService _users;

    public Uoi(IPendingTransactions pending, IUserService users)
    {
        _pending = pending;
        _users = users;
    }

    [Command("uoi"), Summary("I will I will notify someone that they owe you something")]
    public async Task CreatePendingUoi(IUser user, decimal amount, string unit, [Remainder] string? note = null)
    {
        if (amount < 0)
        {
            await TypingReplyAsync("You cannot owe a negative amount!");
        }
        else
        {
            var id = await _pending.CreatePending(Context.User.Id, user.Id, amount, unit, note, DateTime.UtcNow);
            var fid = new BalderHash32(id).ToString();

            await TypingReplyAsync($"{user.Mention} type `!confirm {fid}` to confirm that you owe this");
            await TypingReplyAsync(
                $"{user.Mention} type `!deny {fid}` to deny that you owe this. Please talk to the other user about why!");
        }
    }

    [Command("pay"), Summary("I will record that you have paid someone something")]
    public async Task CreatePendingPayment(IUser user, decimal amount, string unit, [Remainder] string? note = null)
    {
        if (amount < 0)
        {
            await TypingReplyAsync("You cannot pay a negative amount!");
        }
        else
        {
            var id = await _pending.CreatePending(Context.User.Id, user.Id, amount, unit, note, DateTime.UtcNow);
            var fid = new BalderHash32(id).ToString();

            await TypingReplyAsync($"{user.Mention} type `!confirm {fid}` to confirm that you have been paid this");
            await TypingReplyAsync($"{user.Mention} type `!deny {fid}` to deny that you received this payment. Please talk to the other user about why!");
        }
    }

    [Command("confirm"), Summary("I will confirm a pending transaction")]
    public async Task Confirm(string input)
    {
        var fid = BalderHash32.Parse(input);
        if (!fid.HasValue)
        {
            await TypingReplyAsync("Invalid ID `{id}`");
            return;
        }

        var transactions = await _pending.Get(debtId: fid.Value.Value).ToArrayAsync();
        switch (transactions.Length)
        {
            case 0:
                await TypingReplyAsync($"Cannot find a transaction with ID `{fid}`");
                return;
            case > 1:
                await TypingReplyAsync($"Found multiple transactions with ID `{fid}`! This is probably an error, please report it.");
                return;
        }

        var transaction = transactions[0];
        if (transaction.ToId != Context.User.Id)
        {
            await TypingReplyAsync("You cannot confirm this transaction");
            return;
        }

        var result = await _pending.ConfirmPending(fid.Value.Value);
        switch (result)
        {
            case ConfirmResult.Confirmed:
                await TypingReplyAsync($"Confirmed {await transaction.Format(_users)}");
                break;
            case ConfirmResult.AlreadyDenied:
                await TypingReplyAsync("This transaction has already been denied and cannot be confirmed");
                break;
            case ConfirmResult.AlreadyConfirmed:
                await TypingReplyAsync("This transaction has already been confirmed");
                break;
            case ConfirmResult.IdNotFound:
                await TypingReplyAsync($"Cannot find a transaction with ID `{fid}`! This is probably an error, please report it.");
                break;
            default:
                await TypingReplyAsync($"Unknown transaction state `{result}`! This is probably an error, please report it.");
                throw new ArgumentOutOfRangeException();
        }
    }

    [Command("deny"), Summary("I will deny a pending transaction")]
    public async Task Deny(string input)
    {
        var fid = BalderHash32.Parse(input);
        if (!fid.HasValue)
        {
            await TypingReplyAsync($"Invalid ID `{fid}`");
            return;
        }

        var transactions = await _pending.Get(debtId: fid.Value.Value).ToArrayAsync();
        switch (transactions.Length)
        {
            case 0:
                await TypingReplyAsync($"Cannot find a transaction with ID `{fid}`");
                return;
            case > 1:
                await TypingReplyAsync($"Found multiple transactions with ID `{fid}`! This is probably an error, please report it.");
                return;
        }

        var transaction = transactions[0];
        if (transaction.ToId != Context.User.Id)
        {
            await TypingReplyAsync("You cannot deny this transaction");
            return;
        }

        var result = await _pending.DenyPending(fid.Value.Value);
        switch (result)
        {
            case DenyResult.Denied:
                await TypingReplyAsync($"Denied {await transaction.Format(_users, true)}");
                break;
            case DenyResult.AlreadyDenied:
                await TypingReplyAsync("This transaction has already been denied");
                break;
            case DenyResult.AlreadyConfirmed:
                await TypingReplyAsync("This transaction has already been confirmed and can no longer be denied");
                break;
            case DenyResult.IdNotFound:
                await TypingReplyAsync($"Cannot find a transaction with ID `{fid}`! This is probably an error, please report it.");
                break;
            default:
                await TypingReplyAsync($"Unknown transaction state `{result}`! This is probably an error, please report it.");
                throw new ArgumentOutOfRangeException();
        }
    }

    [Command("pending"), Summary("I will list pending transactions you have yet to confirm")]
    public Task Pending()
    {
        return PaginatedPending(
            _pending.Get(toId: Context.User.Id, state: PendingState.Pending),
            "No pending transactions to confirm",
            "You have {0} payments to confirm. Type `!confirm $id` to confirm that it has happened or `!deny $id` otherwise",
            mentionReceiver: false
        );
    }

    [Command("pending-in"), Summary("I will list pending transactions involving you which the other person has no yet confirmed")]
    public Task ReversePending()
    {
        return PaginatedPending(
            _pending.Get(fromId: Context.User.Id, state: PendingState.Pending),
            "No pending transactions involving you for others to confirm",
            "There are {0} unconfirmed payments to you. The other person should type `!confirm $id` or `!deny $id` to confirm or deny that the payment has happened",
            mentionReceiver: true
        );
    }

    private async Task PaginatedPending(IAsyncEnumerable<IPendingTransaction> pending, string none, string paginatedHeader, bool mentionReceiver)
    {
        var pendingArr = await pending.ToListAsync();
        var formatted = new List<string>();
        var longForm = pendingArr.Count < 5;
        foreach (var item in pendingArr)
            formatted.Add(await FormatSinglePending(item));

        if (pendingArr.Count == 0)
            await TypingReplyAsync(none);
        else if (longForm)
            await ReplyAsync(string.Join("\n", formatted));
        else
        {
            await TypingReplyAsync(string.Format(paginatedHeader, pendingArr.Count));
            await PagedReplyAsync(new PaginatedMessage { Pages = formatted.Batch(7).Select(d => string.Join("\n", d)) });
        }

        return;

        async Task<string> FormatSinglePending(IPendingTransaction p)
        {
            var receiver = await _users.Name(p.ToId, mention: mentionReceiver);
            var payer = await _users.Name(p.FromId);
            var note = string.IsNullOrEmpty(p.Note) ? "" : $"'{p.Note}'";
            var amount = TransactionFormatting.FormatCurrency(p.Amount, p.Unit);

            var fid = new BalderHash32(p.Id).ToString();
            return longForm
                ? $"{receiver} Type `!confirm {fid}` or `!deny {fid}` to confirm/deny transaction of {amount} from {payer} {note}"
                : $"`{fid}`: {payer} paid {amount} to {receiver} {note}";
        }
    }
}