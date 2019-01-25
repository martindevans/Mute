using System.Threading.Tasks;
using Discord.WebSocket;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Authorization;
using Mute.Moe.Extensions;

namespace Mute.Moe.Auth
{
    public class BotOwnerRequirement
        : IAuthorizationRequirement
    {
    }

    public class BotOwnerRequirementHandler
        : AuthorizationHandler<BotOwnerRequirement>
    {
        private readonly DiscordSocketClient _client;

        public BotOwnerRequirementHandler(DiscordSocketClient client)
        {
            _client = client;
        }

        protected override async Task HandleRequirementAsync([NotNull] AuthorizationHandlerContext context, BotOwnerRequirement requirement)
        {
            var discordUser = context.User.TryGetDiscordUser(_client);
            if (discordUser == null)
                return;

            var info = await _client.GetApplicationInfoAsync();
            if (info.Owner?.Id == discordUser.Id)
                context.Succeed(requirement);
        }
    }
}
