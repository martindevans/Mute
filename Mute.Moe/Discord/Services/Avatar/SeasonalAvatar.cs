using Discord;
using Discord.WebSocket;
using Mute.Moe.Services.Notifications.Cron;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Discord.Services.Avatar;

public class SeasonalAvatar
    : IAvatarPicker
{
    private readonly ICron _cron;
    private readonly DiscordSocketClient _discord;
    private readonly IReadOnlyList<AvatarConfig.AvatarSet>? _config;
    private readonly Random _rng;

    private CancellationTokenSource? _cts;

    public SeasonalAvatar(ICron cron, DiscordSocketClient discord, Configuration config)
    {
        _cron = cron;
        _discord = discord;
        _rng = new Random();

        if (config.Avatar?.Avatars == null)
            return;
        _config = config.Avatar.Avatars;
    }

    public async Task<AvatarPickResult> PickAvatarNow()
    {
        var avatars = await GetOptions();
        var avatar = avatars.Random(_rng);
        return await SetAvatarNow(avatar);
    }

    public async Task<AvatarPickResult> SetAvatarNow(string path)
    {
        var avatars = await GetOptions();
        if (!avatars.Contains(path))
            return new AvatarPickResult(avatars, null);

        Console.WriteLine($"Setting avatar to `{path}`");
        await using (var stream = File.OpenRead(path))
        {
            var image = new Image(stream);
            await _discord.CurrentUser.ModifyAsync(self => self.Avatar = image);
        }

        return new AvatarPickResult(avatars, path);
    }

    public Task<string[]> GetOptions()
    {
        var now = DateTime.UtcNow.Date.DayOfYear;

        // Get all sets that apply, if any are exclusive remove all non exclusive sets
        var allsets = _config!.Where(a => a.StartDay <= now && a.EndDay >= now).ToList();
        var exclusive = allsets.Where(a => a.Exclusive).ToList();
        var sets = exclusive.Count != 0 ? exclusive : allsets;

        var exts = new[] { "*.bmp", "*.png", "*.jpg", "*.jpeg" };
        var avatars = sets
                     .Where(a => a.Path != null && Directory.Exists(a.Path))
                     .SelectMany(a => exts.SelectMany(e => Directory.GetFiles(a.Path!, e)))
                     .Distinct()
                     .Order()
                     .ToArray();

        return Task.FromResult(avatars);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Do not start avatar update job if no avatar sets are configured
        if ((_config?.Count ?? 0) == 0)
            return;

        _cts = new CancellationTokenSource();
        _ = _cron.Interval(TimeSpan.FromHours(7), PickAvatarNow, int.MaxValue, _cts.Token);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
            await _cts.CancelAsync();
    }
}