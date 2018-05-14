using System;
using System.Threading.Tasks;
using Discord.Addons.Interactive;
using Discord.Commands;
using Mute.Extensions;

namespace Mute.Modules
{
    [Group("vote")]
    public class Democracy
        : InteractiveBase
    {
        [Command("call"), Summary("I will start running a new vote")]
        public async Task CreateVote(params string[] options)
        {
            //todo: check if a vote it already running
            //todo: insert vote options into database

            await this.TypingReplyAsync($"{Context.User.Mention} use '!vote end' to close voting and count the results");
            await this.TypingReplyAsync($"@here use '!vote' to choose one of these options:");
            for (var i = 0; i < options.Length; i++)
                await this.TypingReplyAsync($"{i}.  {options[i]}");
        }

        [Command("end"), Summary("I will stop running the current vote")]
        public async Task EndVote()
        {
            //todo: count votes

            await this.TypingReplyAsync("I don't really know what this is meant to do... :/");

        }

        [Command, Summary("I will record a vote for the currently running vote")]
        public async Task Vote(string choice)
        {
            var isFromVoteOwner = true;
            if (choice.ToLowerInvariant() == "end" && isFromVoteOwner)
            {
                await EndVote();
                return;
            }

            //todo: update vote for this user

            await this.TypingReplyAsync("Uh, no one has told me what this is meant to do. Sorry about that :\\");
        }
    }
}
