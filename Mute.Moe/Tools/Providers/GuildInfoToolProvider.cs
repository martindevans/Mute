using Discord;
using Discord.WebSocket;
using Mute.Moe.Services.ImageGen;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mute.Moe.Tools.Providers;

/// <summary>
/// LLM tools which provide information about Discord guilds the bot is in
/// </summary>
public class GuildInfoToolProvider
    : IToolProvider
{
    private readonly DiscordSocketClient _client;
    private readonly IImageAnalyser _imageAnalyser;
    private readonly HttpClient _http;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Create a new <see cref="GuildInfoToolProvider"/>
    /// </summary>
    /// <param name="client"></param>
    /// <param name="imageAnalyser"></param>
    /// <param name="http"></param>
    public GuildInfoToolProvider(DiscordSocketClient client, IImageAnalyser imageAnalyser, IHttpClientFactory http)
    {
        _client = client;
        _imageAnalyser = imageAnalyser;
        _http = http.CreateClient();

        Tools =
        [
            new AutoTool("guild_info", isDefault:false, BasicGuildInfo),
            new AutoTool("guild_icon_info", isDefault:false, GuildIconInfo),
        ];
    }

    /// <summary>
    /// Get information about a specific guild/server:
    ///  - Name: Name of guild.
    ///  - ID: Unique ID of guild.
    ///  - Description: Description of guild.
    ///  - Member Count: Number of members in this guild (may be null for large guilds)
    ///  - Channels: Summary of channels in guild.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="id">The name or unique ID of the guild</param>
    /// <returns></returns>
    private async Task<object> BasicGuildInfo(ITool.CallContext ctx, string id)
    {
        var guild = await TryFindGuild(id);
        if (guild == null)
            return await CannotFindGuildError(id);

        // Limit length of description
        var desc = guild.Description;
        if (desc.Length > 128)
        {
            var sliced = desc.Length - 128;
            var shortened = $"{desc[..128]}... ({sliced} chars removed)";

            // it's possible the extra stuff we added at the end made it longer.
            // Don't use it if that's the case!
            if (shortened.Length < desc.Length)
                desc = shortened;
        }

        // Ensure we have an accurate count
        await guild.DownloadUsersAsync();
        var userCount = guild.ApproximateMemberCount;

        // Build a small/summary object for each channel
        var channels = new List<object>();
        foreach (var channel in await guild.GetChannelsAsync())
        {
            channels.Add(new
            {
                name = channel.Name,
                id = channel.Id,
                type = channel.ChannelType
            });
        }

        return new
        {
            name = guild.Name,
            id = guild.Id,
            description = desc,
            approx_members = userCount,
            channels = channels
        };
    }

    /// <summary>
    /// Get information about the icon of a specific guild/server, including a description.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="id">The name or unique ID of the guild</param>
    /// <returns></returns>
    private async Task<object> GuildIconInfo(ITool.CallContext ctx, string id)
    {
        var guild = await TryFindGuild(id);
        if (guild == null)
            return await CannotFindGuildError(id);

        var avatar = await _http.GetStreamAsync(guild.IconUrl);
        var description = await _imageAnalyser.GetImageDescription(avatar);

        return new
        {
            guild_id = guild.Id,
            description = description,
        };
    }

    private async Task<object> CannotFindGuildError(string id)
    {
        var similar = await FindSimilarGuilds(id, 3).ToArrayAsync();

        return new
        {
            error = "Could not find guild",
            similar = similar
        };
    }

    private async Task<IGuild?> TryFindGuild(string id)
    {
        // Assume it's a numeric ID
        if (ulong.TryParse(id, out var @ulong))
        {
            var result = _client.GetGuild(@ulong);
            if (result != null)
                return result;
        }

        // Assume name
        foreach (var guild in _client.Guilds)
        {
            if (string.Equals(guild.Name, id, StringComparison.OrdinalIgnoreCase))
                return guild;
        }

        return null;
    }

    private IAsyncEnumerable<IGuild> FindSimilarGuilds(string id, int k)
    {
        // Calculate levenshtein distance to the different types of name, take the best matches
        return (
                   from guild in _client.Guilds
                   let name_dist = guild.Name.Levenshtein(id)
                   let similarity = -(int)name_dist
                   select (guild, similarity)
               )
              .ToAsyncEnumerable()
              .MaxNByKey(k, a => a.similarity)
              .Select(a => a.guild)
              .Reverse();
    }
}