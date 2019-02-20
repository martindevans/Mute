using System.Linq;
using System.Threading.Tasks;
using Discord;
using JetBrains.Annotations;
using Oddity.API.Models.DetailedCore;

namespace Mute.Moe.Services.Information.SpaceX.Extensions
{
    public static class DetailedCoreInfoExtensions
    {
        public static async Task<EmbedBuilder> DiscordEmbed([NotNull] this DetailedCoreInfo info)
        {
            //Choose color based on status
            Color color;
            switch (info.Status)
            {
                case DetailedCoreStatus.Active:
                    color = Color.Green;
                    break;
                case DetailedCoreStatus.Lost:
                    color = Color.Red;
                    break;
                case DetailedCoreStatus.Inactive:
                    color = Color.Orange;
                    break;
                case DetailedCoreStatus.Unknown:
                case null:
                default:
                    color = Color.Purple;
                    break;
            }

            //Create text description
            var description = "";
            if (info.Missions.Count > 0)
            {
                var ms = string.Join(", ", info.Missions.Select(m => $"{m.Name} (Flight {m.Flight})").ToArray());
                description = $"This core has flown {info.Missions.Count} missions; {ms}. ";
            }
            description += info.Details ?? "";

            var builder = new EmbedBuilder()
                          .WithTitle(info.CoreSerial)
                          .WithDescription(description)
                          .WithColor(color)
                          .WithFooter("🚀 https://github.com/r-spacex/SpaceX-API");

            if (info.Block.HasValue)
                builder = builder.AddField("Block", info.Block.Value.ToString());

            if (info.AsdsLandings.HasValue && info.AsdsLandings.Value > 0)
                builder = builder.AddField("ASDS Landings", info.AsdsLandings.Value.ToString());

            if (info.RtlsLandings.HasValue && info.RtlsLandings.Value > 0)
                builder = builder.AddField("RTLS Landings", info.RtlsLandings.Value.ToString());

            if (info.OriginalLaunch.HasValue)
                builder = builder.AddField("First Launch Date", info.OriginalLaunch.Value.ToString("HH\\:mm UTC dd-MMM-yyyy"));

            return builder;
        }
    }
}
