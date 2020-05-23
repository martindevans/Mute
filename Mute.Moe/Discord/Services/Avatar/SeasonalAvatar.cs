using Discord;
using Discord.WebSocket;
using Mute.Moe.Extensions;
using Mute.Moe.Services.Notifications.Cron;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;


namespace Mute.Moe.Discord.Services.Avatar
{
    public class SeasonalAvatar
    {
        private readonly DiscordSocketClient _discord;
        private readonly AvatarConfig _config;
        private readonly Random _rng;

        public SeasonalAvatar( ICron cron,  DiscordSocketClient discord,  Configuration config)
        {
            _discord = discord;
            _config = config.Avatar;
            _rng = new Random();

            // Do not start avatar update job if no avatar sets are configured
            if ((_config?.Avatars?.Length ?? 0) != 0)
                cron.Interval(TimeSpan.FromDays(1), PickDaily, int.MaxValue);
        }

        public async Task<SeasonalAvatarPickResult> PickDaily()
        {
            var now = DateTime.UtcNow.Date.DayOfYear;

            var exts = new string[] { "*.bmp", "*.png", "*.jpg", "*.jpeg" };
            var avatars = _config.Avatars
                .Where(a => a.StartDay <= now && a.EndDay >= now)
                .Where(a => Directory.Exists(a.Path))
                .SelectMany(a => exts.SelectMany(e => Directory.GetFiles(a.Path, e)))
                .Distinct()
                .ToArray();
            Console.WriteLine($"Found {avatars.Length} options: " + string.Join("\n", avatars));

            var avatar = avatars.Random(_rng);
            if (avatar == null)
                return new SeasonalAvatarPickResult(avatars, null);

            Console.WriteLine($"Setting avatar to `{avatar}`");
            using (var stream = File.OpenRead(avatar))
            {
                var image = new Image(stream);
                await _discord.CurrentUser.ModifyAsync(self => self.Avatar = image);
            }

            return new SeasonalAvatarPickResult(avatars, avatar);
        }
    }

    public class SeasonalAvatarPickResult
    {
        public IReadOnlyList<string> Options { get; }
        public string? Choice { get; }

        public SeasonalAvatarPickResult( IReadOnlyList<string> options, string? choice)
        {
            Options = options;
            Choice = choice;
        }
    }
}
