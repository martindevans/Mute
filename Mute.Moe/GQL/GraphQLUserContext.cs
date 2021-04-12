using System.Security.Claims;
using System.Threading.Tasks;
using Discord.WebSocket;
using GraphQL.Server.Transports.AspNetCore;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Mute.Moe.GQL
{
    public class GraphQLUserContextBuilder
        : IUserContextBuilder
    {
        public async Task<object> BuildUserContext(HttpContext httpContext)
        {
            var discord = (DiscordSocketClient)httpContext.RequestServices.GetRequiredService(typeof(DiscordSocketClient));

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
