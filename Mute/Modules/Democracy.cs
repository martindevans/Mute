using System.Data.Common;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    [Group("vote")]
    public class Democracy
        : InteractiveBase
    {
        private readonly DatabaseService _database;

        public Democracy(DatabaseService database)
        {
            _database = database;

            //Create table of votes
            Exec("CREATE TABLE `VoteId` (`ID` INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE, `OwnerId` INTEGER NOT NULL, `IsOpen` INTEGER NOT NULL, `ChannelId` TEXT NOT NULL);");
            Exec("CREATE TABLE `Votes` (`VoteId` INTEGER NOT NULL, `VoterId` INTEGER NOT NULL, `Chosen` TEXT NOT NULL, FOREIGN KEY(`VoteId`) REFERENCES `Votes`(`ID`));");
        }

        private void Exec(string sql)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.ExecuteNonQuery();
            }
        }

        private Task<DbDataReader> ExecReader(string sql)
        {
            using (var cmd = _database.CreateCommand())
            {
                cmd.CommandText = sql;
                return cmd.ExecuteReaderAsync();
            }
        }

        [Command("call"), Summary("I will start running a new vote"), Priority(1)]
        public async Task CreateVote(params string[] options)
        {
            //todo: check if a vote it already running
            //todo: insert vote options into database

            await this.TypingReplyAsync($"{Context.User.Mention} use '!vote end' to close voting and count the results");
            await this.TypingReplyAsync($"Use '!vote' to choose one of these options:");
            for (var i = 0; i < options.Length; i++)
                await this.TypingReplyAsync($"{i}.  {options[i]}");
        }

        [Command("end"), Summary("I will stop running the current vote"), Priority(1)]
        public async Task EndVote()
        {
            //todo: count votes

            await this.TypingReplyAsync("I don't really know what this is meant to do... :/");

        }

        [Command, Summary("I will record a vote for the currently running vote"), Priority(0)]
        public async Task Vote(string choice)
        {
            //todo: update vote for this user

            await this.TypingReplyAsync("Uh, no one has told me what this is meant to do. Sorry about that :\\");
        }
    }
}
