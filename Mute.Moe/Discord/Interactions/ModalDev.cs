//using System;
//using System.Linq;
//using System.Threading.Tasks;
//using Discord;
//using Discord.Interactions;
//using JetBrains.Annotations;
//using Mute.Moe.Discord.Attributes;

//namespace Mute.Moe.Discord.Interactions;

//[UsedImplicitly]
//public class ModalDev
//    : InteractionModuleBase
//{
//    // Registers a command that will respond with a modal.
//    [SlashCommand("modal2", "Dev command for testing modal interactions.")]
//    //public async Task Command() => await Context.Interaction.RespondWithModalAsync<IouDevModal>("9FF87038-31CE-4130-B9BA-8DF453871118");
//    public async Task Command()
//    {
//        try
//        {
//            var modal = new ModalBuilder()
//                       .WithCustomId("9FF87038-31CE-4130-B9BA-8DF453871118")
//                       .WithTitle("IOU Dev Modal")
//                       .AddComponents(new ActionRowBuilder().WithSelectMenu("user", type: ComponentType.UserSelect).Build().Components.ToList(), 0)
//                       .Build();

//            await RespondWithModalAsync(modal);
//        }
//        catch (Exception e)
//        {
//            Console.WriteLine(e);
//            throw;
//        }
//    }

//    // Responds to the modal.
//    [ModalInteraction("9FF87038-31CE-4130-B9BA-8DF453871118", true)]
//    public async Task ModalResponse(Modal modal)
//    {
//        //await RespondAsync(modal.Target.Username);
//        //await RespondAsync(modal.Amount.ToString());
//        //await RespondAsync(modal.Reason);
//        //await RespondAsync(modal.Currency);

//        //// Check if "Why??" field is populated
//        //var reason = string.IsNullOrWhiteSpace(modal.Reason)
//        //    ? "."
//        //    : $" because {modal.Reason}";

//        //// Build the message to send.
//        //var message = "hey everyone, I just learned " +
//        //              $"{Context.User.Mention}'s favorite food is " +
//        //              $"{modal.Food}{reason}";

//        //// Specify the AllowedMentions so we don't actually ping everyone.
//        //AllowedMentions mentions = new()
//        //{
//        //    AllowedTypes = AllowedMentionTypes.Users,
//        //};

//        //// Respond to the modal.
//        //await RespondAsync(message, allowedMentions: mentions, ephemeral: false);

        

        

        

//    }
//}

////// Defines the modal that will be sent.
////[InteractionModal]
////public class IouDevModal
////    : IModal
////{
////    public string Title => "IOU Test Modal";

////    [RequiredInput]
////    [InputLabel("User")]
////    public IUser Target { get; set; } = null!;

////    [RequiredInput]
////    [InputLabel("Amount")]
////    public decimal Amount { get; set; }

////    [RequiredInput]
////    [InputLabel("Currency")]
////    [ModalTextInput("currency", minLength: 1, maxLength: 128)]
////    public string Currency { get; set; } = "GBP";

////    [RequiredInput(false)]
////    [InputLabel("Reason")]
////    [ModalTextInput("reason", minLength: 1, maxLength: 128)]
////    public string? Reason { get; set; } = null;
////}