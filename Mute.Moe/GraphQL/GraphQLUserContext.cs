using System.Security.Claims;
using System.Threading.Tasks;
using Discord.WebSocket;
using GraphQL.Server.Transports.AspNetCore;
using JetBrains.Annotations;
using Microsoft.AspNetCore.Http;

namespace Mute.Moe.GraphQL
{
    public class GraphQLUserContextBuilder
        : IUserContextBuilder
    {
        [ItemNotNull] public async Task<object> BuildUserContext([NotNull] HttpContext httpContext)
        {
            var discord = (DiscordSocketClient)httpContext.RequestServices.GetService(typeof(DiscordSocketClient));

            return new GraphQLUserContext(httpContext.User, discord);
        }
    }

    public class GraphQLUserContext
    {
        public ClaimsPrincipal ClaimsPrincipal { get; }
        public DiscordSocketClient DiscordClient { get; }

        public GraphQLUserContext(ClaimsPrincipal claimsPrincipal, DiscordSocketClient discordClient)
        {
            ClaimsPrincipal = claimsPrincipal;
            DiscordClient = discordClient;
        }
    }
}
