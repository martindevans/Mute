using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Discord.Services.Audio.Playback;
using Mute.Moe.Discord.Services.Responses;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Speech.TTS;

namespace Mute.Moe.Discord.Modules.Introspection
{
    [RequireOwner]
    public class Administration
        : BaseModule
    {
        private readonly DiscordSocketClient _client;
        private readonly ConversationalResponseService _conversations;
        private readonly ITextToSpeech _tts;
        private readonly MultichannelAudioService _audio;

        private SimpleQueueChannel<string> _ttsChannel;

        public Administration(DiscordSocketClient client, ConversationalResponseService conversations, ITextToSpeech tts, MultichannelAudioService audio)
        {
            _client = client;
            _conversations = conversations;
            _tts = tts;
            _audio = audio;

            _ttsChannel = new SimpleQueueChannel<string>();
            _audio.Open(_ttsChannel);
        }

        [Command("say"), Summary("I will say whatever you want, but I won't be happy about it >:(")]
        public async Task Say([NotNull] string message, IMessageChannel channel = null)
        {
            if (channel == null)
                channel = Context.Channel;

            await channel.TypingReplyAsync(message);
        }

        [Command("conversation-status"), Summary("I will show the status of my current conversation with a user")]
        public async Task ConversationState([CanBeNull] IGuildUser user = null)
        {
            if (user == null)
                user = Context.Message.Author as IGuildUser;

            if (user == null)
                await TypingReplyAsync("No user!");
            else
            {
                var c = _conversations.GetConversation(user);
                if (c == null)
                    await TypingReplyAsync("No active conversation");
                else if (c.IsComplete)
                    await TypingReplyAsync($"Conversation is complete `{c.GetType()}`");
                else
                {
                    await TypingReplyAsync($"Conversation is active `{c.GetType()}`...");
                    await ReplyAsync(c.ToString());
                }
            }
        }

        [Command("presence"), Summary("I will set my presence")]
        public async Task SetPresence(ActivityType activity, [CanBeNull, Remainder] string presence)
        {
            if (!string.IsNullOrEmpty(presence))
                await _client.SetActivityAsync(new Game(presence, activity));
        }

        [Command("status"), Summary("I will set my status")]
        public async Task SetPresence(UserStatus status)
        {
            await _client.SetStatusAsync(status);
        }

        [Command("kill"), Alias("die", "self-destruct", "terminate"), Summary("I will immediately terminate my process ⊙︿⊙")]
        public Task Kill(int exitCode = -1)
        {
            Environment.Exit(exitCode);

            return Task.CompletedTask;
        }

        [Command("nickname"), Alias("nick"), Summary("Set my nickname")]
        public async Task Nickname([Remainder] string name)
        {
            await Context.Guild.CurrentUser.ModifyAsync(a => a.Nickname = name);
        }

        [Command("tts")]
        public async Task TextToSpeech([Remainder] string message)
        {
            if (!await _audio.MoveChannel(Context.User))
                await ReplyAsync("You are not in a voice channel");
            else
            {
                var audio = (await _tts.Synthesize(message)).Open();

                await _ttsChannel.Enqueue(message, audio);

                if (audio is IDisposable ad)
                    ad.Dispose();
            }
        }
    }
}
