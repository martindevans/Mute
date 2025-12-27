using Discord;
using Discord.WebSocket;
using Mute.Moe.Discord.Services.Users;
using Mute.Moe.Services.ImageGen;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mute.Moe.Tools.Providers;

/// <summary>
/// Provide information about users
/// </summary>
public class UserInfoToolProvider
    : IToolProvider
{
    private readonly IUserService _users;
    private readonly DiscordSocketClient _client;
    private readonly IImageAnalyser _imageAnalyser;
    private readonly HttpClient _http;

    /// <inheritdoc />
    public IReadOnlyList<ITool> Tools { get; }

    /// <summary>
    /// Create a new <see cref="UserInfoToolProvider"/>
    /// </summary>
    /// <param name="users"></param>
    /// <param name="client"></param>
    /// <param name="imageAnalyser"></param>
    /// <param name="http"></param>
    public UserInfoToolProvider(IUserService users, DiscordSocketClient client, IImageAnalyser imageAnalyser, IHttpClientFactory http)
    {
        _users = users;
        _client = client;
        _imageAnalyser = imageAnalyser;
        _http = http.CreateClient();

        Tools =
        [
            new AutoTool("user_info", isDefault:false, BasicUserInfo),
            new AutoTool("user_avatar_info", isDefault:false, UserAvatarInfo),
        ];
    }

    /// <summary>
    /// Get information about a specific user:
    ///  - Display name: Their chosen name.
    ///  - Username: Their login username.
    ///  - Nickname: Their chosen nickname in the current context.
    ///  - ID: Unique ID for this user.
    ///  - Bot: Indicate if this user is a bot.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="id">The name, nickname, username or numeric ID of the user</param>
    /// <returns></returns>
    private async Task<object> BasicUserInfo(ITool.CallContext ctx, string id)
    {
        var user = await TryFindUser(ctx, id);
        if (user == null)
            return await CannotFindUserError(ctx, id);

        return new
        {
            displayname = user.GlobalName,
            username = user.Username,
            nickname = (user as IGuildUser)?.Nickname,
            id = user.Id,
            bot = user.IsBot,
            avatar_id = user.AvatarId,
        };
    }

    /// <summary>
    /// Get information about the avatar of a specific user, including a description.
    /// </summary>
    /// <param name="ctx"></param>
    /// <param name="id">The name, nickname, username or numeric ID of the user</param>
    /// <returns></returns>
    private async Task<object> UserAvatarInfo(ITool.CallContext ctx, string id)
    {
        var user = await TryFindUser(ctx, id);
        if (user == null)
            return await CannotFindUserError(ctx, id);

        var avatar = await _http.GetStreamAsync(user.GetDisplayAvatarUrl(ImageFormat.Png));
        var description = await _imageAnalyser.GetImageDescription(avatar);

        return new
        {
            user_id = user.Id,
            description = description,
        };
    }

    private static async Task<object> CannotFindUserError(ITool.CallContext ctx, string id)
    {
        var similar = await FindSimilarUsersInChannel(ctx, id, 3)
                           .Select(a => new
                            {
                               name = a.Item2,
                               id = a.Item1.Id,
                            }).ToArrayAsync();

        return new
        {
            error = "Could not find user",
            similar = similar
        };
    }

    private async Task<IUser?> TryFindUser(ITool.CallContext ctx, string id)
    {
        IUser? result;

        // Assume it's a numeric ID
        if (ulong.TryParse(id, out var @ulong))
        {
            result = _client.GetUser(@ulong);
            if (result != null)
                return result;
        }

        // Assume username
        result = _client.GetUser(id);
        if (result != null)
            return result;

        // Try a guild nickname
        var guild = (ctx.Channel as IGuildChannel)?.Guild;
        if (guild != null)
        {
            var guildUsers = await guild.GetUsersAsync();

            foreach (var guildUser in guildUsers)
            {
                var match = string.Equals(guildUser.Nickname, id, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(guildUser.GlobalName, id, StringComparison.OrdinalIgnoreCase)
                         || string.Equals(guildUser.Username, id, StringComparison.OrdinalIgnoreCase);

                if (match)
                    return guildUser;
            }
        }

        return null;
    }

    private static IAsyncEnumerable<(IUser, string)> FindSimilarUsersInChannel(ITool.CallContext ctx, string id, int k)
    {
        // Calculate levenshtein distance to the different types of name, take the best matches
        return (
            from user in ctx.Channel.GetUsersAsync().SelectMany(a => a)
            let global_dist = user.GlobalName?.Levenshtein(id) ?? int.MaxValue
            let username_dist = user.Username.Levenshtein(id)
            let nick = (user as IGuildUser)?.Nickname
            let nick_dist = nick?.Levenshtein(id) ?? uint.MaxValue
            let best = MostSimilar(user.GlobalName, global_dist, user.Username, username_dist, nick, nick_dist)
            let similarity = -(int)best.distance
            select (user, similarity, best.name)
        ).MaxNByKey(k, a => a.similarity).Reverse().Select(a => (a.user, a.name));

        static (string name, uint distance) MostSimilar(string g, uint gd, string u, uint ud, string n, uint nd)
        {
            var best = g;
            var bestDist = gd;

            if (ud < bestDist)
            {
                best = u;
                bestDist = ud;
            }

            if (nd < bestDist)
            {
                best = n;
                bestDist = nd;
            }

            return (best, bestDist);
        }
    }
}