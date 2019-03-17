using System.Threading.Tasks;
using Discord.WebSocket;
using GraphQL.Authorization;
using JetBrains.Annotations;
using Mute.Moe.Extensions;
using Mute.Moe.GQL;

namespace Mute.Moe.Auth.GraphQL
{
    public class InBotGuild
        : IAuthorizationRequirement
    {
        private readonly DiscordSocketClient _client;

        public InBotGuild(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task Authorize([NotNull] AuthorizationContext context)
        {
            var userCtx = (GraphQLUserContext)context.UserContext;
            var user = userCtx.ClaimsPrincipal;

            if (!await user.IsInBotGuild(_client))
                context.ReportError("Not in any mutual guilds with bot");
        }
    }
}
