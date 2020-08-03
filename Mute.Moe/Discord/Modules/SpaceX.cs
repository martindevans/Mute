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
        private readonly Random _rng;

        public SpaceX(ISpacexInfo spacex, ISpacexNotifications notifications, Random rng)
        {
            _spacex = spacex;
            _notifications = notifications;
            _rng = rng;
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

            if (details.Launches != null && details.Launches.Count > 1)
            {
                var embed2 = await details.AugmentDiscordEmbed(embed);
                await msg.ModifyAsync(p => p.Embed = embed2.Build());
            }
        }

        [Command("details"), Alias("flight-no", "flight-num", "mission", "flight"), Summary("I will tell you about a specific spacex launch")]
        public async Task LaunchDetails(uint id)
        {
            var launch = await _spacex.Launch(id);
            if (launch == null)
            {
                await TypingReplyAsync("There doesn't seem to be a flight by that ID");
                return;
            }

            await ReplyAsync(launch.DiscordEmbed(_rng));
        }

        [Command("next"), Alias("upcoming"), Summary("I will tell you about the next spacex launch(es)")]
        public async Task NextLaunches(int count = 1)
        {
            if (count == 1)
            {
                await ReplyAsync(await _spacex.NextLaunch().DiscordEmbed(_rng));
            }
            else
            {
                var launches = (await _spacex.Upcoming()).Where(a => a.DateUtc.HasValue).OrderBy(a => a.FlightNumber).Take(count).ToArray();
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
            await ReplyAsync(await _spacex.Roadster().DiscordEmbed(_rng));
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
            var next = (await _spacex.Upcoming()).Where(a => a.DateUtc.HasValue).OrderBy(a => a?.DateUtc ?? DateTime.MaxValue).Take(count).ToArray();

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

                yield return new Key("spacex",
                    new Decomposition("*next*launch*", d => NextLaunch(1)!),
                    new Decomposition("#*launches*", d => NextLaunch(int.Parse(d[0]))!),
                    new Decomposition("*launches*#", d => NextLaunch(int.Parse(d[0]))!)
                );
            }
        }
    }
}
