using System.Threading.Tasks;
using Discord.WebSocket;

using Microsoft.AspNetCore.Authorization;
using Mute.Moe.Extensions;

namespace Mute.Moe.Auth.Asp
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

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, BotOwnerRequirement requirement)
        {
            if (await context.User.IsBotOwner(_client))
                context.Succeed(requirement);
        }
    }
}
