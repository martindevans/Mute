using Discord;
using Discord.Interactions;
using Mute.Moe.Services.Payment;
using System.Globalization;
using System.Threading.Tasks;
using Mute.Moe.Discord.Interaction;

namespace Mute.Moe.Discord.Modules.Payment;

/// <summary>
/// Provides an "IOU" user interaction (right click on their name). Shows a modal that allows the user to create an IOU transaction.
/// </summary>
[UsedImplicitly]
public class Iou2
    : MuteInteractionModuleBase
{
    private readonly ITransactions _transactions;

    /// <summary>
    /// Create a new <see cref="Iou2"/>
    /// </summary>
    /// <param name="transactions"></param>
    public Iou2(ITransactions transactions)
    {
        _transactions = transactions;
    }

    /// <summary>
    /// Show the IOU modal for a target user
    /// </summary>
    /// <param name="targetUser"></param>
    /// <returns></returns>
    [UserCommand("IOU")]
    [UsedImplicitly]
    public async Task UserIou(IUser targetUser)
    {
        await RespondWithModalAsync(IouModal.InteractionId, new IouModal(targetUser));
    }

    /// <summary>
    /// Handle the IOU modal being submitted
    /// </summary>
    /// <param name="modal"></param>
    /// <returns></returns>
    [ModalInteraction(IouModal.InteractionId, ignoreGroupNames:true)]
    [UsedImplicitly]
    public async Task ModalResponse(IouModal modal)
    {
        await DeferAsync();

        // Check users
        var src = Context.User;
        var target = modal.Target;
        if (target == null)
        {
            await ReplyAsync2("Target user cannot be null! **Cancelled transaction**");
            return;
        }

        // Parse amount
        if (!decimal.TryParse(modal.Amount?.Trim(), NumberStyles.Number, CultureInfo.InvariantCulture, out var amount))
        {
            await ReplyAsync2("Cannot parse amount as a number! **Cancelled transaction**");
            return;
        }
        
        // Validate amount
        if (amount < 0)
        {
            await ReplyAsync2("You cannot owe a negative amount! **Cancelled transaction**");
            return;
        }

        await _transactions.CreateTransaction(
            target.Id,
            src.Id,
            amount,
            modal.Currency,
            modal.Reason ?? "",
            DateTime.UtcNow
        );

        await ReplyAsync2($"{Context.User.Mention} owes {TransactionFormatting.FormatCurrency(amount, modal.Currency)} to {target.Mention}");
    }
}

/// <summary>
/// Modal popup for IOU interactions
/// </summary>
public class IouModal
    : IModal
{
    /// <summary>
    /// Interaction ID for this modal
    /// </summary>
    public const string InteractionId = "5E763C73-69FF-46DA-A72E-F06C4CFDDFD4";

    /// <inheritdoc />
    public string Title => "IOU Transaction";

    /// <summary>
    /// Destination user of this transaction
    /// </summary>
    [RequiredInput]
    [InputLabel("Target")]
    [ModalUserSelect("target")]
    public IUser? Target { get; set; }

    /// <summary>
    /// Monetary amount
    /// </summary>
    [RequiredInput]
    [InputLabel("Amount")]
    [ModalTextInput("amount", TextInputStyle.Short, "123", 1, 10)]
    public string Amount { get; set; } = "";

    /// <summary>
    /// The currency symbol
    /// </summary>
    [RequiredInput]
    [InputLabel("Currency")]
    [ModalTextInput("currency", TextInputStyle.Short, "GBP", 1, 32, "GBP")]
    public string Currency { get; set; } = "GBP";

    /// <summary>
    /// The transaction reason string
    /// </summary>
    [RequiredInput(false)]
    [InputLabel("Reason")]
    [ModalTextInput("reason", minLength: 1, maxLength: 128)]
    public string? Reason { get; set; } = null;

    /// <summary>
    /// Create modal with prefilled target property
    /// </summary>
    /// <param name="target"></param>
    public IouModal(IUser target)
    {
        Target = target;
    }

    /// <summary>
    /// Create modal, required for construction by Discord.net
    /// </summary>
    [UsedImplicitly]
    public IouModal()
    {
    }
}