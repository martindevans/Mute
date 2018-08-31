using System;
using System.Threading.Tasks;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    [Group]
    [RequireOwner]
    public class Administration
        : ModuleBase
    {
        private readonly DatabaseService _database;
        private readonly SentimentService _sentiment;

        public Administration(DatabaseService database, SentimentService sentiment)
        {
            _database = database;
            _sentiment = sentiment;
        }

        [Command("hostinfo"), Summary("I Will tell you where I am being hosted")]
        public async Task HostName()
        {
            await this.TypingReplyAsync($"Machine: {Environment.MachineName}");
            await this.TypingReplyAsync($"User: {Environment.UserName}");
            await this.TypingReplyAsync($"OS: {Environment.OSVersion}");
        }

        [Command("say"), Summary("I will say whatever you want, but I won't be happy about it >:(")]
        [RequireOwner]
        public async Task Say([Remainder] string s2)
        {
            await this.TypingReplyAsync(s2);
        }

        [Command("sql"), Summary("I will execute an arbitrary SQL statement. Please be very careful x_x")]
        [RequireOwner]
        public async Task Sql([Remainder] string sql)
        {
            using (var result = await _database.ExecReader(sql))
                await this.TypingReplyAsync($"SQL affected {result.RecordsAffected} rows");
        }
    }
}
