using System.Threading.Tasks;
using Discord;
using Discord.Interactions;

namespace Mute.Moe.Discord.Interactions;

[UsedImplicitly]
public class UserInfo
    : MuteInteractionModuleBase
{
    [UserCommand("Show Avatar")]
    public async Task ShowAvatar(IUser user)
    {
        var url = user.GetAvatarUrl(ImageFormat.Png, 2048);
        await RespondAsync(url);
    }
}