using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using GraphQL.Authorization;
using GraphQL.Types;

using Mute.Moe.Extensions;
using Mute.Moe.Services.Introspection.Uptime;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.GQL;
using Mute.Moe.GQL.Schema;

namespace Mute.Moe.Services.Introspection
{
    public class Status
    {
        private readonly IUptime _uptime;
        public TimeSpan Uptime => _uptime.Uptime;

        private readonly DiscordSocketClient _client;
        public TimeSpan Latency => TimeSpan.FromMilliseconds(_client.Latency);
        public int Shard => _client.ShardId;
        public IReadOnlyCollection<IGuild> Guilds => _client.Guilds;

        public long MemoryWorkingSet => Environment.WorkingSet;
        public long TotalGCMemory => GC.GetTotalMemory(false);

        public Status(IUptime uptime, DiscordSocketClient client)
        {
            _uptime = uptime;
            _client = client;
        }
    }

    public class StatusSchema
        : ObjectGraphType<Status>, InjectedSchema.IRootQuery
    {
        public StatusSchema()
        {
            this.AuthorizeWith("DiscordUser");

            Field(x => x.Uptime);
            Field(x => x.Latency);
            Field(x => x.Shard);
            Field(x => x.MemoryWorkingSet).AuthorizeWith("BotOwner");
            Field(x => x.TotalGCMemory).AuthorizeWith("BotOwner");

            Field<ListGraphType<IGuildSchema>>("guilds",
                resolve: GetGuilds,
                arguments: new QueryArguments(
                    new QueryArgument(typeof(ListGraphType<StringGraphType>)) {Name = "id_in"}
                )).AuthorizeWith("InAnyBotGuild");
        }

        private static async Task<IReadOnlyCollection<IGuild>> GetGuilds(ResolveFieldContext<Status> context)
        {
            var userCtx = (GraphQLUserContext)context.UserContext;
            var user = userCtx.ClaimsPrincipal;
            var client = userCtx.DiscordClient;

            //If they are not a discord user, return an empty list
            var dUser = user.TryGetDiscordUser(client);
            if (dUser == null)
                return new IGuild[0];

            //We'll only reveal bots the user knows about, unless they're the bot owner
            var list = await user.IsBotOwner(client) ? context.Source.Guilds : dUser.MutualGuilds;

            //Get the filter argument, early exit if it's not set
            if (!context.Arguments.TryGetValue("id_in", out var value))
                return list;

            //Pull all the ulong we can parse out of the filter list
            var filter = new HashSet<ulong>(((List<object>)value).OfType<string>().Select(a => ulong.TryParse(a, out var r) ? (ulong?)r : null).Where(a => a.HasValue).Select(a => a!.Value).ToArray());

            //filter the list by that query
            return list.Where(a => filter.Contains(a.Id)).ToArray();
        }

        public void Add(IServiceProvider services, ObjectGraphType ogt)
        {
            ogt.Field<StatusSchema>(
                "status",
                resolve: context => services.GetService<Status>()
            );
        }
    }
}
