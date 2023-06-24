using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Humanizer;
using JetBrains.Annotations;
using Mute.Moe.Discord.Services.Users;

namespace Mute.Moe.Discord.Modules.Introspection;

[UsedImplicitly]
public class UserInfo
    : BaseModule
{
    private readonly IUserService _users;
    private readonly HttpClient _http;

    public UserInfo(IHttpClientFactory http, IUserService users)
    {
        _users = users;
        _http = http.CreateClient();
    }

    [Command("userid"), Summary("I will type out the ID of the specified user")]
    public async Task UserId(IUser? user = null)
    {
        user ??= Context.User;

        await TypingReplyAsync($"User ID for {user.Username} is `{user.Id}`");
    }

    [Command("whois"), Summary("I will print out a summary of information about the given user")]
    public async Task Whois(IUser? user = null)
    {
        await TypingReplyAsync(GetUserInfo(user ?? Context.User));
    }

    [Command("avatar"), Summary("I will show the avatar for a user")]
    public async Task Avatar(IUser? user = null)
    {
        var u = user ?? Context.User;
        var url = u.GetAvatarUrl(ImageFormat.Png, 2048);

        using var resp = await _http.GetAsync(url);
        var m = new MemoryStream();
        await resp.Content.CopyToAsync(m);
        m.Position = 0;

        await Context.Channel.SendFileAsync(m, $"{_users.Name(u)}.png");
    }

    private static string GetUserInfo(IUser user)
    {
        var str = new StringBuilder($"{user.Username}");

        var gu = user as IGuildUser;

        if (gu?.Nickname != null)
            str.Append($" AKA {gu.Nickname}");

        var clause = 0;
        if (user.IsBot && !user.Username.StartsWith('*'))
        {
            str.Append(" is a bot");
            clause++;
        }

        var activities = gu?.Activities;
        if (activities?.Count >= 1)
        {
            var activity = activities.First();
            str.Append($" is currently {activity.Type} {activity.Name}");
            clause++;
        }

        if (gu?.JoinedAt != null)
        {
            if (clause > 0)
                str.Append(" and");

            var duration = (DateTime.UtcNow - gu.JoinedAt.Value.UtcDateTime).Humanize();

            str.Append($" has been a member for {duration}");
        }

        return str.ToString();
    }
}