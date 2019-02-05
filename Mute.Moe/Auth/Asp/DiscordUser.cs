using System.Threading.Tasks;
using Discord.WebSocket;
using JetBrains.Annotations;
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

        [NotNull] protected override async Task HandleRequirementAsync([NotNull] AuthorizationHandlerContext context, DiscordUserRequirement requirement)
        {
            if (context.User.TryGetDiscordUser(_client) != null)
                context.Succeed(requirement);
        }
    }
}
