using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using Mute.Moe.Discord.Attributes;

namespace Mute.Moe.Discord.Interactions;

[UsedImplicitly]
public class Utility
    : InteractionModuleBase
{
    [SlashCommand("ping", "Check that I am awake")]
    public async Task Ping()
    {
        await RespondAsync("pong");
    }

    // Registers a command that will respond with a modal.
    [SlashCommand("modal", "Tell us about your favorite food.")]
    public async Task Command() => await Context.Interaction.RespondWithModalAsync<FoodModal>("food_menu");

    // Responds to the modal.
    [ModalInteraction("food_menu", true)]
    public async Task ModalResponse(FoodModal modal)
    {
        // Check if "Why??" field is populated
        var reason = string.IsNullOrWhiteSpace(modal.Reason)
            ? "."
            : $" because {modal.Reason}";

        // Build the message to send.
        var message = "hey everyone, I just learned " +
                      $"{Context.User.Mention}'s favorite food is " +
                      $"{modal.Food}{reason}";

        // Specify the AllowedMentions so we don't actually ping everyone.
        AllowedMentions mentions = new()
        {
            AllowedTypes = AllowedMentionTypes.Users,
        };

        // Respond to the modal.
        await RespondAsync(message, allowedMentions: mentions, ephemeral: false);
    }
}

// Defines the modal that will be sent.
[InteractionModal]
public class FoodModal
    : IModal
{
    public string Title => "Test Modal";

    // Strings with the ModalTextInput attribute will automatically become components.
    [InputLabel("Input A")]
    [ModalTextInput("food_name", placeholder: "Pizza", maxLength: 20)]
    public string? Food { get; set; }

    // Additional paremeters can be specified to further customize the input.    
    // Parameters can be optional
    [RequiredInput(false)]
    [InputLabel("Input B")]
    [ModalTextInput("food_reason", TextInputStyle.Paragraph, "It's tasty", maxLength: 500)]
    public string? Reason { get; set; }
}