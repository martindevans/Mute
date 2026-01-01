using BalderHash;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Users;
using Mute.Moe.Services.Payment;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Modules.Payment;

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

[UsedImplicitly]
[HelpGroup("payment")]
[WarnDebugger]
[TypingReply]
[Summary("Administration for IOU system")]
public class PaymentAdministration
    : MuteBaseModule
{
    private readonly IPendingTransactions _pending;
    private readonly IUserService _users;

    public PaymentAdministration(IPendingTransactions pending, IUserService users)
    {
        _pending = pending;
        _users = users;
    }

    [Command("iou-pending-tsx-info"), Summary("I will tell you about a transaction by ID"), UsedImplicitly]
    public async Task PendingTsxInfo(string id)
    {
        if (BalderHash32.Parse(id) is not BalderHash32 hash)
        {
            await ReplyAsync("Cannot parse ID");
            return;
        }

        var debt = await _pending.Get(debtId: hash.Value).ToArrayAsync();

        switch (debt.Length)
        {
            case 0:
                await ReplyAsync("Cannot find that pending transaction");
                break;

            case 1:
            {
                var formatted = await Uoi.FormatSinglePending(debt.Single(), _users, false, true);
                await ReplyAsync(formatted);
                break;
                }

            default:
                await ReplyAsync("More than one pending transaction with this ID! This is a bug, tell Martin.");
                break;
        }
    }
}