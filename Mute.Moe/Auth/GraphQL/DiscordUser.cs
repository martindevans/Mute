using System.Threading.Tasks;
using Discord.WebSocket;
using GraphQL.Authorization;
using JetBrains.Annotations;
using Mute.Moe.Extensions;
using Mute.Moe.GraphQL;

namespace Mute.Moe.Auth.GraphQL
{
    public class DiscordUser
        : IAuthorizationRequirement
    {
        private readonly DiscordSocketClient _client;

        public DiscordUser(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task Authorize([NotNull] AuthorizationContext context)
        {
            var userCtx = (GraphQLUserContext)context.UserContext;
            var user = userCtx.ClaimsPrincipal;

            if (user.TryGetDiscordUser(_client) == null)
                context.ReportError("Not logged in with Discord");
        }
    }
}
