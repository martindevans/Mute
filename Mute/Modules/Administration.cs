using System;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Extensions;
using Mute.Services;
using Mute.Services.Responses;
using Newtonsoft.Json.Linq;

namespace Mute.Modules
{
    [Group]
    [RequireOwner]
    public class Administration
        : BaseModule
    {
        private readonly DiscordSocketClient _client;
        private readonly IDatabaseService _database;
        private readonly HistoryLoggingService _history;
        private readonly ConversationalResponseService _conversations;
        private readonly WordVectorsService _wordVectors;

        public Administration(DiscordSocketClient client, IDatabaseService database, HistoryLoggingService history, ConversationalResponseService conversations, WordVectorsService wordVectors)
        {
            _client = client;
            _database = database;
            _history = history;
            _conversations = conversations;
            _wordVectors = wordVectors;
        }

        [Command("hostinfo"), Summary("I Will tell you where I am being hosted")]
        public async Task HostName()
        {
            var embed = new EmbedBuilder()
                .AddField("Machine", Environment.MachineName)
                .AddField("User", Environment.UserName)
                .AddField("OS", Environment.OSVersion)
                .AddField("CPUs", Environment.ProcessorCount)
                .Build();

            await ReplyAsync("", false, embed);
        }

        [Command("say"), Summary("I will say whatever you want, but I won't be happy about it >:(")]
        [RequireOwner]
        public async Task Say([NotNull] string message, IMessageChannel channel = null)
        {
            if (channel == null)
                channel = Context.Channel;

            await channel.TypingReplyAsync(message);
        }

        [Command("sql"), Summary("I will execute an arbitrary SQL statement. Please be very careful x_x")]
        [RequireOwner]
        public async Task Sql([Remainder] string sql)
        {
            using (var result = await _database.ExecReader(sql))
                await TypingReplyAsync($"SQL affected {result.RecordsAffected} rows");
        }

        [Command("subscribe"), Summary("I will subscribe history logging to a new channel")]
        public async Task Scrape([NotNull] ITextChannel channel)
        {
            try
            {
                await _history.BeginMonitoring(channel);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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

        [Command("leave-voice"), Summary("I will immediately leave the voice channel (if you are in one)")]
        public async Task LeaveVoice()
        {
            if (Context.User is IVoiceState v)
            {
                using (await v.VoiceChannel.ConnectAsync())
                    await Task.Delay(100);
            }
            else
            {
                await ReplyAsync("You are not in a voice channel");
            }
        }

        [Command("test-wv")]
        public async Task TestWv(string word)
        {
            var result = await _wordVectors.GetVector(word);
            var json = JArray.FromObject(result).ToString(Newtonsoft.Json.Formatting.None);
            await ReplyAsync(json.Substring(0, Math.Min(500, json.Length)));
        }

        [Command("test-wv-cos")]
        public async Task TestWvCos(string a, string b)
        {
            var result = await _wordVectors.CosineDistance(a, b);
            await ReplyAsync(result.ToString());
        }

        [Command("test-wv-stats")]
        public async Task TestWvStats()
        {
            await ReplyAsync(new EmbedBuilder().AddField("Size", _wordVectors.CacheCount).AddField("Hits", _wordVectors.CacheHits).AddField("Miss", _wordVectors.CacheMisses));
        }
    }
}
