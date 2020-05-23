using System.Threading.Tasks;
using Discord.WebSocket;

using Microsoft.AspNetCore.Authorization;
using Mute.Moe.Extensions;

namespace Mute.Moe.Auth.Asp
{
    public class DiscordUserRequirement
        : IAuthorizationRequirement
    {
    }

    public class DiscordUserRequirementHandler
        : AuthorizationHandler<DiscordUserRequirement>
    {
        private readonly DiscordSocketClient _client;

        public DiscordUserRequirementHandler(DiscordSocketClient client)
        {
            _client = client;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, DiscordUserRequirement requirement)
        {
            if (context.User.TryGetDiscordUser(_client) != null)
                context.Succeed(requirement);
        }
    }
}
