using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL.Types;
using JetBrains.Annotations;
using Mute.Moe.GraphQL.Schema;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Extensions;
using Mute.Moe.GraphQL;

namespace Mute.Moe.Services.Reminders
{
    public interface IReminders
    {
        /// <summary>
        /// Create a new reminder
        /// </summary>
        /// <returns></returns>
        [NotNull, ItemNotNull] Task<IReminder> Create(DateTime triggerTime, string prelude, string msg, ulong channelId, ulong userId);

        /// <summary>
        /// Get all reminders in date order filtered by user, time range, channel or status and limited by a max count
        /// </summary>
        /// <returns></returns>
        [NotNull, ItemNotNull] Task<IOrderedAsyncEnumerable<IReminder>> Get(ulong? userId = null, DateTime? after = null, DateTime? before = null, ulong? channel = null, uint? count = null);

        /// <summary>
        /// Delete a reminder
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<bool> Delete(uint id);

        /// <summary>
        /// Action invoked when a new reminder is created
        /// </summary>
        event Action<IReminder> ReminderCreated;

        /// <summary>
        /// Action invoked when a reminder is deleted
        /// </summary>
        event Action<uint> ReminderDeleted;
    }

    public interface IReminder
    {
        uint ID { get; }
        ulong UserId { get; }
        ulong ChannelId { get; }

        string Prelude { get; }
        string Message { get; }

        DateTime TriggerTime { get; }
    }

    public class RemindersSchema
        : InjectedSchema.IRootQuery
    {
        private async Task<IReadOnlyList<IReminder>> GetReminders(IReminders reminders, [NotNull] ResolveFieldContext<object> context)
        {
            var userCtx = (GraphQLUserContext)context.UserContext;
            var user = userCtx.ClaimsPrincipal;
            var client = userCtx.DiscordClient;

            //If they are not a discord user, return an empty list
            var dUser = user.TryGetDiscordUser(client);
            if (dUser == null)
                return Array.Empty<IReminder>();

            DateTime? before = null;
            if (context.Arguments.TryGetValue("before_unix", out var beforeUnix))
                before = ((ulong)(int)beforeUnix).FromUnixTimestamp();

            DateTime? after = null;
            if (context.Arguments.TryGetValue("after_unix", out var afterUnix))
                after = ((ulong)(int)afterUnix).FromUnixTimestamp();

            return await reminders.Get(userId: dUser.Id, after: after, before: before).ToArray();
        }

        public void Add(IServiceProvider services, ObjectGraphType ogt)
        {
            var reminders = services.GetService<IReminders>();

            ogt.Field<ListGraphType<IReminderSchema>>(
                "reminders",
                resolve: context => GetReminders(reminders, context),
                arguments: new QueryArguments(
                    new QueryArgument(typeof(IntGraphType)) { Name = "before_unix" },
                    new QueryArgument(typeof(IntGraphType)) { Name = "after_unix" }
                )
            );
        }
    }

    // ReSharper disable once InconsistentNaming
    public class IReminderSchema
        : ObjectGraphType<IReminder>
    {
        public IReminderSchema()
        {
            Field(typeof(UIntGraphType), "channelId", resolve: x => x.Source.ID);
            Field("id", x => new FriendlyId32(x.ID).ToString());
            Field("userid", x => x.UserId.ToString());

            Field(x => x.Message);
            Field(x => x.Prelude);
            Field(x => x.TriggerTime);
            
        }
    }
}
