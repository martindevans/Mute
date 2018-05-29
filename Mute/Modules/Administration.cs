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

        public Administration(DatabaseService database)
        {
            _database = database;
        }

        [Command("say"), Summary("I will say whatever you want, but I won't be happy about it >:(")]
        [RequireOwner]
        public async Task Say([Remainder] string s)
        {
            await this.TypingReplyAsync(s);
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
