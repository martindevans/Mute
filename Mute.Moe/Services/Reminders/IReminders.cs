using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using GraphQL.Types;
using Humanizer;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Extensions;
using Mute.Moe.GQL;
using Mute.Moe.GQL.Schema;

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
        Task<bool> Delete(ulong userId, uint id);

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

    public class RemindersQuerySchema
        : InjectedSchema.IRootQuery
    {
        private static async Task<IReadOnlyList<IReminder>> GetReminders(IReminders reminders, [NotNull] ResolveFieldContext<object> context)
        {
            var userCtx = (GraphQLUserContext)context.UserContext;
            var user = userCtx.ClaimsPrincipal;
            var client = userCtx.DiscordClient;

            //If they are not a discord user, return an empty list
            var dUser = user.TryGetDiscordUser(client);
            if (dUser == null)
                return Array.Empty<IReminder>();

            DateTime? before = null;
            if (context.Arguments.TryGetValue("before", out var beforeObj) && beforeObj is DateTime beforeTime)
                before = beforeTime;

            DateTime? after = null;
            if (context.Arguments.TryGetValue("after", out var afterObj) && afterObj is DateTime afterTime)
                after = afterTime;

            return await reminders.Get(userId: dUser.Id, after: after, before: before).ToArray();
        }

        public void Add(IServiceProvider services, ObjectGraphType ogt)
        {
            var reminders = services.GetService<IReminders>();

            ogt.Field<ListGraphType<IReminderSchema>>(
                "reminders",
                resolve: context => GetReminders(reminders, context),
                arguments: new QueryArguments(
                    new QueryArgument(typeof(DateTimeGraphType)) { Name = "before" },
                    new QueryArgument(typeof(DateTimeGraphType)) { Name = "after" }
                )
            );
        }
    }

    public class RemindersMutationSchema
        : InjectedSchema.IRootMutation
    {
        [ItemCanBeNull] private async Task<IReminder> CreateReminder(IReminders reminders, [NotNull] ResolveFieldContext<object> context)
        {
            var userCtx = (GraphQLUserContext)context.UserContext;
            var user = userCtx.ClaimsPrincipal;
            var client = userCtx.DiscordClient;

            //If they are not a discord user, return null
            var dUser = user.TryGetDiscordUser(client);
            if (dUser == null)
                return null;

            //Get message or early exit
            if (!context.Arguments.TryGetValue("message", out var messageObj) || !(messageObj is string message))
                return null;

            //Get trigger time or exit if none specified
            if (!context.Arguments.TryGetValue("trigger_time", out var triggerTime) || !(triggerTime is DateTime trigger))
                return null;

            //Check if we're trying to schedule an event in the past
            if (trigger < DateTime.UtcNow)
                return null;

            //Get channel or exit if none specified or user cannot write into this channel
            if (context.Arguments.TryGetValue("channel_id", out var channelIdStr) && ulong.TryParse(channelIdStr as string ?? "", out var channelId))
            {
                //Get the channel or exit if it doesn't exist
                var channel = client.GetChannel(channelId) as ITextChannel;
                if (channel == null)
                    return null;

                //Check if user is in channel
                var gUser = await channel.GetUserAsync(dUser.Id);
                if (gUser == null)
                    return null;

                //check user has permission to write in this channel
                var permission = gUser.GetPermissions(channel);
                if (!permission.Has(ChannelPermission.SendMessages))
                    return null;
            }
            else
                return null;

            var prelude = $"{dUser.Mention} Reminder from {DateTime.UtcNow.Humanize(dateToCompareAgainst: trigger, culture: CultureInfo.GetCultureInfo("en-gn"))}...";

            return await reminders.Create(trigger, prelude, message, channelId, dUser.Id);
        }

        public void Add(IServiceProvider services, ObjectGraphType ogt)
        {
            var reminders = services.GetService<IReminders>();

            ogt.Field<IReminderSchema>("create_reminder",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<DateTimeGraphType>> { Name = "trigger_time" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "message" },
                    new QueryArgument<NonNullGraphType<StringGraphType>> { Name = "channel_id" }
                ),
                resolve: context => CreateReminder(reminders, context)
            );
        }
    }

    // ReSharper disable once InconsistentNaming
    public class IReminderSchema
        : ObjectGraphType<IReminder>
    {
        public IReminderSchema()
        {
            Field(typeof(UIntGraphType), "channel_id", resolve: x => x.Source.ID);
            Field(typeof(DateTimeGraphType), "trigger_time", resolve: x => x.Source.TriggerTime);

            Field("id", x => new FriendlyId32(x.ID).ToString());
            Field("userid", x => x.UserId.ToString());

            Field(x => x.Message);
            Field(x => x.Prelude);
        }
    }
}
