using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Interactions;
using JetBrains.Annotations;
using Mute.Moe.Discord.Services.Users;

namespace Mute.Moe.Discord.Interactions;

[UsedImplicitly]
public class UserInfo
    : MuteInteractionModuleBase
{
    public UserInfo(IHttpClientFactory http, IUserService users)
    {
        _users = users;
        _http = http.CreateClient();
    }

    [UserCommand("Show Avatar")]
    public async Task ShowAvatar(IUser user)
    {
        var url = user.GetAvatarUrl(ImageFormat.Png, 2048);
        await RespondAsync(url);
    }
}