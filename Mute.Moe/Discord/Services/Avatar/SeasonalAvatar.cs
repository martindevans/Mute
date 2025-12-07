using Discord;
using Discord.WebSocket;
using Mute.Moe.Services.Notifications.Cron;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Mute.Moe.Discord.Services.Avatar;

/// <summary>
/// Picks avatars based on the date
/// </summary>
public class SeasonalAvatar
    : IAvatarPicker
{
    private readonly ICron _cron;
    private readonly DiscordSocketClient _discord;
    private readonly IReadOnlyList<AvatarConfig.AvatarSet>? _config;
    private readonly Random _rng;

    private CancellationTokenSource? _cts;

    /// <summary>
    /// Create a new seasonal avatar picker than will auto pick new avatars
    /// </summary>
    /// <param name="cron"></param>
    /// <param name="discord"></param>
    /// <param name="config"></param>
    public SeasonalAvatar(ICron cron, DiscordSocketClient discord, Configuration config)
    {
        _cron = cron;
        _discord = discord;
        _rng = new Random();

        if (config.Avatar?.Avatars == null)
            return;
        _config = config.Avatar.Avatars;
    }

    /// <summary>
    /// Force immediate repick of Avatar
    /// </summary>
    /// <returns></returns>
    public async Task<AvatarPickResult> PickAvatarNow()
    {
        var avatars = await GetOptions();
        var avatar = avatars.Random(_rng);
        return await SetAvatarNow(avatar);
    }

    /// <summary>
    /// Force set a specific avatar
    /// </summary>
    /// <param name="path"></param>
    /// <returns></returns>
    public async Task<AvatarPickResult> SetAvatarNow(string path)
    {
        var avatars = await GetOptions();
        if (!avatars.Contains(path))
            return new AvatarPickResult(avatars, null);

        Log.Information("Setting avatar to: `{0}`", path);
        await using (var stream = File.OpenRead(path))
        {
            var image = new Image(stream);
            await _discord.CurrentUser.ModifyAsync(self => self.Avatar = image);
        }

        return new AvatarPickResult(avatars, path);
    }

    /// <inheritdoc />
    public Task<string[]> GetOptions()
    {
        var now = DateTime.UtcNow.Date.DayOfYear;

        // Get all sets that apply, if any are exclusive remove all non exclusive sets
        var validSets = _config!.Where(a => a.StartDay <= now && a.EndDay >= now).ToList();
        var exclusiveSets = validSets.Where(a => a.Exclusive).ToList();
        var sets = exclusiveSets.Count != 0 ? exclusiveSets : validSets;

        var exts = new[] { "*.bmp", "*.png", "*.jpg", "*.jpeg" };
        var avatars = sets
                     .Where(a => a.Path != null && Directory.Exists(a.Path))
                     .SelectMany(a => exts.SelectMany(e => Directory.GetFiles(a.Path!, e)))
                     .Distinct()
                     .Order()
                     .ToArray();

        return Task.FromResult(avatars);
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Do not start avatar update job if no avatar sets are configured
        if ((_config?.Count ?? 0) == 0)
            return;

        _cts = new CancellationTokenSource();
        _ = _cron.Interval(TimeSpan.FromHours(7), PickAvatarNow, int.MaxValue, _cts.Token);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_cts != null)
            await _cts.CancelAsync();
    }
}