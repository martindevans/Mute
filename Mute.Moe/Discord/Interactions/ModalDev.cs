//using System.Threading.Tasks;
//using Discord;
//using Discord.Interactions;
//using Discord.WebSocket;
//using JetBrains.Annotations;

//namespace Mute.Moe.Discord.Interactions;

//[UsedImplicitly]
//public class ModalDev
//    : MuteInteractionModuleBase
//{
//    private readonly DiscordSocketClient _client;

//    public ModalDev(DiscordSocketClient client)
//    {
//        _client = client;
//    }

//    [SlashCommand("modal2", "Dev command for testing modal interactions.")]
//    public async Task Command()
//    {
//        await RespondWithModalAsync<IouDevModal>(IouDevModal.ID);
//    }

//    // Responds to the modal.
//    [ModalInteraction(IouDevModal.ID, true)]
//    public async Task ModalResponse(IouDevModal modal)
//    {
//        await DeferAsync();

//        var message = "modal received " + modal.Currency;

//        var confirmed = await InteractionUtility.ConfirmAsync(_client, Context.Channel, TimeSpan.FromMinutes(1), "xxx" + message);
//        if (!confirmed)
//        {
//            await RespondAsync("Confirmation timed out. Cancelled transaction!");
//            return;
//        }

//        await ModifyOriginalResponseAsync(msg =>
//        {
//            msg.Content = message;
//        });
//    }
//}

//public class IouDevModal
//    : IModal
//{
//    public const string ID = "9FF87038-31CE-4130-B9BA-8DF453871118";

//    public string Title => "IOU Test Modal";

//    [RequiredInput]
//    [InputLabel("User")]
//    public IUser Target { get; set; } = null!;

//    [RequiredInput]
//    [InputLabel("Amount")]
//    [ModalTextInput("amount", TextInputStyle.Short, "123", 1, 10)]
//    public string Amount { get; set; } = "";

//    [RequiredInput]
//    [InputLabel("Currency")]
//    [ModalTextInput("currency", minLength: 1, maxLength: 128)]
//    public string Currency { get; set; } = "GBP";

//    [RequiredInput(false)]
//    [InputLabel("Reason")]
//    [ModalTextInput("reason", minLength: 1, maxLength: 128)] 
//    public string? Reason { get; set; } = null;
//}