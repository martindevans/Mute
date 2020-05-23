using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Imitation;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules
{
    public class Imitation
        : BaseModule
    {
        private readonly IImitationModelProvider _models;

        public Imitation(IImitationModelProvider models)
        {
            _models = models;
        }

        [Command("imitate"), Summary("I will speak with the voice of another user")]
        public async Task ImitateAsync( IUser user, string? prompt = null)
        {
            var model = await _models.GetModel(user);

            if (model == null)
                await TypingReplyAsync($"I don't have a model for {user.Username}, please train one with `imitate-train`");
            else
            {
                var reply = await model.Predict(prompt);
                var embed = new EmbedBuilder()
                            .WithTitle(reply)
                            .WithAuthor(user)
                            .WithColor(Color.DarkOrange);

                await ReplyAsync(embed);
            }
        }

        [RequireOwner, Command("imitate-train"), Summary("I will begin training an imitation model for the given user"), ThinkingReply(EmojiLookup.Loading)]
        public async Task ImitateTrainAsync( IUser user)
        {
            var model = await _models.GetModel(user);
            if (model != null)
            {
                await TypingReplyAsync($"{Name(user)} already has a trained model!");
                return;
            }

            // Reply with a message that status updates will be appended to
            var msg = await TypingReplyAsync($"Beginning training for {Name(user)} (this may take a while).");

            // Train model
            await _models.BeginTraining(user, Context.Channel, async status => {
                await msg.ModifyAsync(m => m.Content = msg.Content + "\n - " + status);
                await Task.Delay(100);
            });
        }
    }
}
