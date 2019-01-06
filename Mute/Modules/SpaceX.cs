using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Discord.Commands;
using Mute.Services;
using System.Linq;
using Discord;
using Humanizer;
using JetBrains.Annotations;
using Mute.Services.Responses.Eliza;
using Mute.Services.Responses.Eliza.Engine;
using Oddity.API.Models.Launch;

namespace Mute.Modules
{
    [Group("spacex")]
    public class SpaceX
        : BaseModule, IKeyProvider
    {
        private readonly SpacexService _spacex;

        public SpaceX(SpacexService spacex)
        {
            _spacex = spacex;
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

            await ReplyAsync(LaunchEmbed(launches.Single()));
        }

        [Command("next"), Alias("upcoming"), Summary("I will tell you about the next spacex launch(es)")]
        public async Task NextLaunches(int count = 1)
        {
            try
            {
                if (count == 1)
                {
                    await ReplyAsync(LaunchEmbed(await _spacex.NextLaunch()));
                }
                else
                {
                    var info = await DescribeUpcomingFlights(count);
                    foreach (var item in info)
                        await TypingReplyAsync(item);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [Command("roadster"), Summary("I will tell you about the spacex roadster")]
        public async Task Roadster()
        {
            var roadster = await _spacex.Roadster();

            var speed = ((int)roadster.SpeedKph);
            var earth = ((int)roadster.EarthDistanceKilometers);
            var mars = ((int)roadster.MarsDistanceKilometers);
            var period = ((int)roadster.PeriodDays).Days().Humanize();

            await TypingReplyAsync($"{roadster.Name} (NORAD:{roadster.NoradId}) was put into space {roadster.DateTimeUtc.ToLocalTime().Humanize()}. " +
                                   $"It is currently travelling at {speed:#,##0}km/s in a {roadster.OrbitType} orbit, {earth:#,##0}km away from Earth " +
                                   $"and {mars:#,##0}km away from Mars. It completes an orbit every {period}.");
        }

        #region helpers
        [NotNull] private static EmbedBuilder LaunchEmbed([NotNull] LaunchInfo launch)
        {
            var icon = launch.Links.MissionPatch ?? launch.Links.MissionPatchSmall;
            var url = launch.Links.Wikipedia ?? launch.Links.RedditCampaign ?? launch.Links.RedditLaunch ?? launch.Links.Presskit;
            var upcoming = launch.Upcoming.HasValue && launch.Upcoming.Value;
            var success = launch.LaunchSuccess.HasValue && launch.LaunchSuccess.Value;

            var builder = new EmbedBuilder()
                .WithTitle(launch.MissionName)
                .WithDescription(launch.Details)
                .WithUrl(url)
                .WithAuthor($"Flight {launch.FlightNumber}", icon, icon)
                .WithColor(upcoming ? Color.Blue : success ? Color.Green : Color.Red)
                .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API");

            var site = launch.LaunchSite.SiteLongName ?? launch.LaunchSite.SiteName;
            if (!string.IsNullOrWhiteSpace(site))
                builder = builder.AddField("Launch Site", site, false);

            if (launch.LaunchDateUtc.HasValue)
                builder = builder.AddField("Launch Date", launch.LaunchDateUtc.Value.ToString("HH\\:mm UTC MMM-dd-yyyy"), true);

            var landing = string.Join(", ", launch.Rocket.FirstStage.Cores.Select(c => (c.LandingVehicle.HasValue ? c.LandingVehicle.Value.ToString() : null)).Where(a => a != null).ToArray());
            if (!string.IsNullOrWhiteSpace(landing))
                builder = builder.AddField("Landing", landing, true);

            builder = builder.AddField("Vehicle", launch.Rocket.RocketName, true);

            

            return builder;
        }

        [ItemNotNull] private async Task<IReadOnlyList<string>> DescribeUpcomingFlights(int count)
        {
            var next = (await _spacex.Upcoming()).Where(a => a.LaunchDateUtc.HasValue).OrderBy(a => a.LaunchDateUtc.Value).Take(count).ToArray();

            var responses = new List<string>();
            if (next.Length == 1)
            {
                responses.Add(DescribeFlight(next.Single()));
            }
            else
            {
                foreach (var item in next)
                {
                    Debug.Assert(item.LaunchDateUtc != null);
                    var date = item.LaunchDateUtc.Value.Humanize();
                    var num = item.FlightNumber;
                    var site = item.LaunchSite;
                    var name = item.MissionName;
                    var reuse = item.Reuse;
                    var type = item.Rocket.RocketName;

                    var response = $"Flight {num} will launch '{name}' from {site.SiteName} on a{(reuse.Core ?? false ? " reused" : "")} {type} rocket {date}";
                    responses.Add(response);
                }
            }
            return responses;
        }

        [NotNull] private static string DescribeFlight([NotNull] LaunchInfo info)
        {
            var response = info.Details;

            if (info.LaunchDateUtc.HasValue)
            {
                var t = info.LaunchDateUtc.Value.ToLocalTime();
                if (info.LaunchDateUtc.Value < DateTime.UtcNow)
                {
                    response += $" The launch date was {t:yyyy-MM-dd HH:mm} (UK time)";
                }
                else
                {
                    var h = t.Humanize();
                    response += $" The expected launch date is about {h} ({t:yyyy-MM-dd HH:mm} UK time)";
                }
            }

            return response;
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
