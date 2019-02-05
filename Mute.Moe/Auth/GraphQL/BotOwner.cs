using System.Threading.Tasks;
using Discord.WebSocket;
using GraphQL.Authorization;
using JetBrains.Annotations;
using Mute.Moe.Controllers.GraphQL;
using Mute.Moe.Extensions;

namespace Mute.Moe.Auth.GraphQL
{
    public class BotOwner
        : IAuthorizationRequirement
    {
        private readonly DiscordSocketClient _client;

        public BotOwner(DiscordSocketClient client)
        {
            _client = client;
        }

        public async Task Authorize([NotNull] AuthorizationContext context)
        {
            var userCtx = (GraphQLUserContext)context.UserContext;
            var user = userCtx.ClaimsPrincipal;

            if (!await user.IsBotOwner(_client))
                context.ReportError("Not the bot owner");
        }
    }
}
