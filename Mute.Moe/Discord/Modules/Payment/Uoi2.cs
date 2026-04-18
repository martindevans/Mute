using BalderHash;
using Discord;
using Discord.Commands;
using Discord.Interactions;
using Discord.WebSocket;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Interactions;
using Mute.Moe.Discord.Services.Users;
using Mute.Moe.Services.Payment;
using Mute.Moe.Utilities;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Modules.Payment;

/// <summary>
/// 
/// </summary>
[UsedImplicitly]
[HelpGroup("payment")]
[WarnDebugger]
[TypingReply]
public class Uoi2(IPendingTransactions _pending, IUserService _users)
    : MuteBaseModule
{
    /// <summary>
    /// Create a new "pending transaction" indicating that the sending user is owned money by another user. Transaction must be confirmed
    /// by the other user
    /// </summary>
    /// <param name="user"></param>
    /// <param name="amount"></param>
    /// <param name="unit"></param>
    /// <param name="note"></param>
    /// <returns></returns>
    [Command("uoi"), UsedImplicitly]
    public async Task CreatePendingUoi(IUser user, decimal amount, string unit, [Remainder] string? note = null)
    {
        if (amount < 0)
        {
            await TypingReplyAsync("You cannot owe a negative amount!");
        }
        else
        {
            // Sanity check unusual units, user must confirm anything other than "GBP"
            if (!await Iou.CheckUnit(unit, this, Context))
                return;

            // Create the transaction in the DB
            var tsx = await _pending.CreatePending(Context.User.Id, user.Id, amount, unit, note, DateTime.UtcNow);

            // Send the message
            await Uoi2Interaction.SendMessage(Context.Channel, _users, tsx, Uoi2Interaction.PendingType.Uoi);
        }
    }

    /// <summary>
    /// Create a new "pending transaction" indicating that the sender user has paid some money to another user. Transaction must be confirmed
    /// by the other user.
    /// </summary>
    /// <param name="user"></param>
    /// <param name="amount"></param>
    /// <param name="unit"></param>
    /// <param name="note"></param>
    /// <returns></returns>
    [Command("pay"), UsedImplicitly]
    public async Task CreatePendingPayment(IUser user, decimal amount, string unit, [Remainder] string? note = null)
    {
        if (amount < 0)
        {
            await TypingReplyAsync("You cannot pay a negative amount!");
        }
        else
        {
            // Sanity check unusual units, user must confirm anything other than "GBP"
            if (!await Iou.CheckUnit(unit, this, Context))
                return;

            // Create the transaction in the DB
            var tsx = await _pending.CreatePending(Context.User.Id, user.Id, amount, unit, note, DateTime.UtcNow);

            // Send the message
            await Uoi2Interaction.SendMessage(Context.Channel, _users, tsx, Uoi2Interaction.PendingType.Pay);
        }
    }
}

/// <summary>
/// Handle UOI/PAY interactions
/// </summary>
[UsedImplicitly]
public class Uoi2Interaction(IUserService _users, IPendingTransactions _pending)
    : MuteInteractionModuleBase
{
    #region IDs
    private const string Prefix = "PendingTransactions";

    private const string ConfirmIdPrefix = $"{Prefix}_Confirm";
    private const string DenyIdPrefix = $"{Prefix}_Deny";

    private static string CreateConfirmUoiId(ulong transaction, PendingType type)
    {
        return $"{ConfirmIdPrefix}_{transaction}_{type}";
    }

    private static string CreateDenyId(ulong transaction, PendingType type)
    {
        return $"{DenyIdPrefix}_{transaction}_{type}";
    }
    #endregion

    #region interaction callbacks
    /// <summary>
    /// Receive a callback from a UOI/PAY confirm button
    /// </summary>
    /// <param name="paymentId"></param>
    /// <param name="type"></param>
    [ComponentInteraction($"{ConfirmIdPrefix}_*_*", ignoreGroupNames: true)]
    [UsedImplicitly]
    public async Task ConfirmButton(uint paymentId, PendingType type)
    {
        await HandleInteraction(
            paymentId,
            type,
            DoConfirmation
        );

        async Task<PendingState> DoConfirmation(PendingTransaction transaction)
        {
            var fid = new BalderHash32(transaction.Id);
            
            // Do the actual confirmation
            var result = await _pending.ConfirmPending(transaction.Id);
            switch (result)
            {
                case ConfirmResult.Confirmed:
                    await ReplyAsync2($"Confirmed {await transaction.Format(_users)}");
                    return PendingState.Confirmed;
                
                case ConfirmResult.AlreadyDenied:
                    await ReplyAsync2("This transaction has already been denied and cannot be confirmed");
                    return PendingState.Denied;
                
                case ConfirmResult.AlreadyConfirmed:
                    await ReplyAsync2("This transaction has already been confirmed");
                    return PendingState.Confirmed;
                
                case ConfirmResult.IdNotFound:
                    await ReplyAsync2($"Cannot find a transaction with ID `{fid}`! This is probably an error, please report it.");
                    return transaction.State;

                default:
                    await ReplyAsync2($"Unknown transaction state `{result}`! This is probably an error, please report it.");
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    /// <summary>
    /// Receive a callback from a UOI/PAY deny button
    /// </summary>
    /// <param name="paymentId"></param>
    /// <param name="type"></param>
    [ComponentInteraction($"{DenyIdPrefix}_*_*", ignoreGroupNames: true)]
    [UsedImplicitly]
    public async Task DenyButton(uint paymentId, PendingType type)
    {
        await HandleInteraction(
            paymentId,
            type,
            DoDenial
        );

        async Task<PendingState> DoDenial(PendingTransaction transaction)
        {
            var fid = new BalderHash32(transaction.Id);
            
            // Do the actual denial
            var result = await _pending.DenyPending(transaction.Id);
            switch (result)
            {
                case DenyResult.Denied:
                    await ReplyAsync2($"Denied {await transaction.Format(_users, true)}");
                    return PendingState.Denied;
                
                case DenyResult.AlreadyDenied:
                    await ReplyAsync2("This transaction has already been denied");
                    return PendingState.Denied;
                
                case DenyResult.AlreadyConfirmed:
                    await ReplyAsync2("This transaction has already been confirmed and can no longer be denied");
                    return PendingState.Confirmed;
                
                case DenyResult.IdNotFound:
                    await ReplyAsync2($"Cannot find a transaction with ID `{fid}`! This is probably an error, please report it.");
                    return transaction.State;

                default:
                    await ReplyAsync2($"Unknown transaction state `{result}`! This is probably an error, please report it.");
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    private async Task HandleInteraction(uint paymentId, PendingType type, Func<PendingTransaction, Task<PendingState>> work)
    {
        try
        {
            var args = (SocketMessageComponent)Context.Interaction;
            await args.DeferLoadingAsync();

            // Get the transaction
            var transaction = await _pending.GetSingle(paymentId);
            if (transaction == null)
            {
                await ReplyAsync2($"Cannot find a transaction with ID `{new BalderHash32(paymentId)}`");
                return;
            }

            // Check if this person is allowed to confirm it
            if (!transaction.CanUserConfirm(Context.User.Id))
            {
                var payeeMention = await _users.Name(transaction.ToId);
                await ReplyAsync2($"This transaction can only be confirmed by {payeeMention}!");
                return;
            }

            // Do the actual confirm/deny
            var newState = await work(transaction);
            transaction = transaction with { State = newState };

            // Modify original message
            var components = await BuildComponents(_users, transaction, type);
            await args.Message.ModifyAsync(msg =>
            {
                msg.Flags = MessageFlags.ComponentsV2;
                msg.Components = components.Build();
            });
        }
        catch (Exception ex)
        {
            await ReplyAsync2($"Interaction Exception! {ex.Message}");
            throw;
        }
    }
    #endregion

    #region helpers
    /// <summary>
    /// Build the embed with details of this transaction
    /// </summary>
    /// <param name="users"></param>
    /// <param name="transaction"></param>
    /// <param name="type"></param>
    /// <returns></returns>
    private static async Task<ComponentBuilderV2> BuildComponents(IUserService users, PendingTransaction transaction, PendingType type)
    {
        var payer = await users.Name(transaction.FromId, mention:true);
        var payee = await users.Name(transaction.ToId, mention: true);
        var fid = new BalderHash32(transaction.Id).ToString();

        var note = transaction.Note;
        if (string.IsNullOrWhiteSpace(note))
            note = "No description provided";

        // Sidebar colour
        var color = transaction.State switch
        {
            PendingState.Pending => Color.Blue,
            PendingState.Confirmed => Color.Green,
            PendingState.Denied => Color.Red,
            _ => Color.DarkGrey,
        };

        // Title
        var typeName = type.ToString().ToUpperInvariant();
        var title = transaction.State switch
        {
            PendingState.Pending => $"### {typeName} Transaction **PENDING**",
            PendingState.Confirmed => $"### {typeName} Transaction **CONFIRMED** {EmojiLookup.Tick}",
            PendingState.Denied => $"### {typeName} Transaction **DENIED** {EmojiLookup.Cross}",

            _ => $"ERROR Transaction UNKNOWN STATE={transaction.State} {EmojiLookup.Skull}"
        };

        // Description
        var description = type switch
        {
            PendingType.Uoi => $"**{payee} owes {transaction.Amount} {transaction.Unit} to {payer}**",
            PendingType.Pay => $"**{payer} has paid {transaction.Amount} {transaction.Unit} to {payee}**",

            _ => $"This transaction has unknown type `{type}` (this is a bug)."
        };

        // Transaction details
        var container = new ContainerBuilder()
            .WithAccentColor(color)
            .AddComponent(new TextDisplayBuilder(title))
            .AddComponent(new TextDisplayBuilder(description))
            .AddComponent(new TextDisplayBuilder(note))
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder($"{transaction.Instant:f} UTC / `{fid}`"))
            .AddComponent(new SeparatorBuilder());

        // Transaction buttons
        if (transaction.State == PendingState.Pending)
        {
            container.AddComponent(
                new ActionRowBuilder(
                    new ButtonBuilder().WithStyle(ButtonStyle.Primary).WithLabel("Deny").WithCustomId(CreateDenyId(transaction.Id, type)),
                    new ButtonBuilder().WithStyle(ButtonStyle.Success).WithLabel("Confirm").WithCustomId(CreateConfirmUoiId(transaction.Id, type))
                )
            );
        }

        var components = new ComponentBuilderV2();
        components.AddComponent(container);

        return components;
    }

    /// <summary>
    /// Send transaction message
    /// </summary>
    /// <param name="channel"></param>
    /// <param name="users"></param>
    /// <param name="transaction"></param>
    /// <param name="type"></param>
    public static async Task SendMessage(IMessageChannel channel, IUserService users, PendingTransaction transaction, PendingType type)
    {
        await channel.SendMessageAsync(components: (await BuildComponents(users, transaction, type)).Build());
    }
    #endregion

    /// <summary>
    /// Type of pending transaction (controls language used to describe transaction)
    /// </summary>
    public enum PendingType
    {
        /// <summary>
        /// This is a "UOI" transaction
        /// </summary>
        Uoi,

        /// <summary>
        /// This is a "PAY" transaction
        /// </summary>
        Pay,
    }
}