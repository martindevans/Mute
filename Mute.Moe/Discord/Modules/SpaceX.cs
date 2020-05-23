using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;

using Mute.Moe.Discord.Services.Responses.Eliza;
using Mute.Moe.Discord.Services.Responses.Eliza.Engine;
using Mute.Moe.Services.Information.SpaceX;
using Mute.Moe.Services.Information.SpaceX.Extensions;
using Mute.Moe.Services.Notifications.SpaceX;

namespace Mute.Moe.Discord.Modules
{
    [Group("spacex")]
    public class SpaceX
        : BaseModule, IKeyProvider
    {
        private readonly ISpacexInfo _spacex;
        private readonly ISpacexNotifications _notifications;

        public SpaceX(ISpacexInfo spacex, ISpacexNotifications notifications)
        {
            _spacex = spacex;
            _notifications = notifications;
        }

        [Command("core"), Alias("booster"), Summary("I will tell you about a specific SpaceX vehicle")]
        public async Task CoreDetails(string id)
        {
            var details = (await _spacex.Core(id)) ?? await _spacex.Core($"B{id}");
            if (details == null)
            {
                await TypingReplyAsync("There doesn't seem to be a core by that ID");
                return;
            }

            var embed = await details.DiscordEmbed();
            var msg = await ReplyAsync(embed);

            if (details.Missions.Count > 1)
            {
                var embed2 = await details.AugmentDiscordEmbed(embed, _spacex);
                await msg.ModifyAsync(p => p.Embed = embed2.Build());
            }
        }

        [Command("details"), Alias("flight-no", "flight-num"), Summary("I will tell you about a specific spacex launch")]
        public async Task LaunchDetails(int id)
        {
            var launches = await _spacex.Launch(id);
            if (launches == null || launches.Count == 0)
            {
                await TypingReplyAsync("There doesn't seem to be a flight by that ID");
                return;
            }

            if (launches.Count > 1)
            {
                await TypingReplyAsync("There are multiple launches with that ID!?");
                return;
            }

            await ReplyAsync(launches.Single().DiscordEmbed());
        }

        [Command("next"), Alias("upcoming"), Summary("I will tell you about the next spacex launch(es)")]
        public async Task NextLaunches(int count = 1)
        {
            if (count == 1)
            {
                await ReplyAsync(await _spacex.NextLaunch().DiscordEmbed());
            }
            else
            {
                var launches = (await _spacex.Upcoming()).Where(a => a.LaunchDateUtc.HasValue).OrderBy(a => a.FlightNumber).Take(count).ToArray();
                await DisplayItemList(
                    launches,
                    () => "There are no upcoming SpaceX launches!",
                    null,
                    (l, i) => l.Summary()
                );
            }
        }

        [Command("roadster"), Summary("I will tell you about the spacex roadster")]
        public async Task Roadster()
        {
            await ReplyAsync(await _spacex.Roadster().DiscordEmbed());
        }

        [Command("subscribe"), RequireOwner]
        public async Task Subscribe(IRole? role = null)
        {
            await _notifications.Subscribe(Context.Channel.Id, role?.Id);
            await TypingReplyAsync("Subscribed to receive SpaceX mission updates");
        }

        #region helpers
        private async Task<IReadOnlyList<string>> DescribeUpcomingFlights(int count)
        {
            var next = (await _spacex.Upcoming()).Where(a => a.LaunchDateUtc.HasValue).OrderBy(a => a?.LaunchDateUtc ?? DateTime.MaxValue).Take(count).ToArray();

            var responses = new List<string>();
            foreach (var item in next)
            {
                var response = item.Summary();
                responses.Add(response);
            }
            return responses;
        }
        #endregion

        public IEnumerable<Key> Keys
        {
            get
            {
                async Task<string> NextLaunch(int count)
                    => string.Join("\n", await DescribeUpcomingFlights(count));

                yield return new Key("spacex", 10,
                    new Decomposition("*next*launch*", d => NextLaunch(1)),
                    new Decomposition("#*launches*", d => NextLaunch(int.Parse(d[0]))),
                    new Decomposition("*launches*#", d => NextLaunch(int.Parse(d[0])))
                );
            }
        }
    }
}
