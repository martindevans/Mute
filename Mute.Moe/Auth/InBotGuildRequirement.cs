using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Linq;
using Discord.WebSocket;
using JetBrains.Annotations;
using Mute.Moe.Extensions;

namespace Mute.Moe.Auth
{
    public class InBotGuildRequirement
        : IAuthorizationRequirement
    {
    }

    public class InBotGuildRequirementHandler
        : AuthorizationHandler<InBotGuildRequirement>
    {
        private readonly DiscordSocketClient _client;

        public InBotGuildRequirementHandler(DiscordSocketClient client)
        {
            _client = client;
        }

        [NotNull] protected override async Task HandleRequirementAsync([NotNull] AuthorizationHandlerContext context, InBotGuildRequirement requirement)
        {
            var discordUser = context.User.TryGetDiscordUser(_client);
            if (discordUser == null)
                return;

            if (discordUser.MutualGuilds?.Any() ?? false)
                context.Succeed(requirement);
        }
    }
}
