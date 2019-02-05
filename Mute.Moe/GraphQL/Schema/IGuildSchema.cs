using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using GraphQL.Authorization;
using GraphQL.Types;
using JetBrains.Annotations;
using Mute.Moe.Controllers.GraphQL;
using Mute.Moe.Extensions;

namespace Mute.Moe.GraphQL.Schema
{
    // ReSharper disable once InconsistentNaming
    public class IGuildSchema
        : ObjectGraphType<IGuild>
    {
        public IGuildSchema()
        {
            this.AuthorizeWith("DiscordUser");

            Field("id", ctx => ctx.Id.ToString());
            Field("iconId", ctx => ctx.IconId, nullable:true);
            Field("iconUrl", ctx => ctx.IconUrl, nullable:true);
            Field("name", ctx => ctx.Name);

            Field<ListGraphType<IGuildUserSchema>>("members",
                resolve: GetMembers,
                arguments: new QueryArguments(
                    new QueryArgument(typeof(ListGraphType<StringGraphType>)) { Name = "id_in" }
                )).AuthorizeWith("InAnyBotGuild");

            Field<ListGraphType<IRoleSchema>>("roles",
                resolve: GetRoles,
                arguments: new QueryArguments(
                    new QueryArgument(typeof(ListGraphType<StringGraphType>)) { Name = "role_id_in" }
                )).AuthorizeWith("InAnyBotGuild");
        }

        [ItemNotNull] private async Task<IReadOnlyCollection<IRole>> GetRoles([NotNull] ResolveFieldContext<IGuild> context)
        {
            var userCtx = (GraphQLUserContext)context.UserContext;
            var user = userCtx.ClaimsPrincipal;
            var client = userCtx.DiscordClient;

            //If they are not a discord user return an empty list
            var dUser = user.TryGetDiscordUser(client);
            if (dUser == null)
                return Array.Empty<IRole>();

            //Only show results if they are in this guild or the bot owner
            if (!await user.IsBotOwner(client) && !dUser.MutualGuilds.Contains(context.Source))
                return Array.Empty<IRole>();
            
            //Get the list of _all_ members in the guild
            IEnumerable<IRole> roles = context.Source.Roles;

            //Filter by role ID
            if (context.Arguments.TryGetValue("role_id_in", out var idInFilter))
            {
                var roleFilter = GetUlongHashset(idInFilter);
                roles = roles.Where(a => roleFilter.Contains(a.Id));
            }

            //filter the list by that query
            return roles.ToArray();
        }

        [ItemNotNull] private async Task<IReadOnlyCollection<IGuildUser>> GetMembers([NotNull] ResolveFieldContext<IGuild> context)
        {
            var userCtx = (GraphQLUserContext)context.UserContext;
            var user = userCtx.ClaimsPrincipal;
            var client = userCtx.DiscordClient;

            //If they are not a discord user return an empty list
            var dUser = user.TryGetDiscordUser(client);
            if (dUser == null)
                return Array.Empty<IGuildUser>();

            //Only show results if they are in this guild or the bot owner
            if (!await user.IsBotOwner(client) && !dUser.MutualGuilds.Contains(context.Source))
                return Array.Empty<IGuildUser>();
            
            //Get the list of _all_ members in the guild
            var list = await context.Source.GetUsersAsync();

            //Get the filter argument, early exit with all results if it's not set
            if (!context.Arguments.TryGetValue("id_in", out var idInFilter))
                return list;

            //Pull all the ulong we can parse out of the filter list
            var filter = GetUlongHashset(idInFilter);

            //filter the list by that query
            return list.Where(a => filter.Contains(a.Id)).ToArray();
        }

        [NotNull] private static HashSet<ulong> GetUlongHashset([NotNull] object arg)
        {
            return new HashSet<ulong>(((List<object>)arg)
                .OfType<string>()
                .Select(a => ulong.TryParse(a, out var r) ? (ulong?)r : null)
                .Where(a => a.HasValue)
                .Select(a => a.Value)
                .ToArray());
        }
    }
}
