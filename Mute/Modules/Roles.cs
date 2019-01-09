using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Services;
using System;

namespace Mute.Modules
{
    [Group("role"), Alias("roles")]
    public class Roles
        : BaseModule
    {
        private readonly RoleService _roles;

        public Roles(RoleService roles)
        {
            _roles = roles;
        }

        [Command("id"), Summary("I will type out the ID of the specified role")]
        public async Task RoleId([NotNull] IRole role)
        {
            await TypingReplyAsync($"ID for `{role.Name}` is `{role.Id}`");
        }

        [Command("join"), Summary("I will give you the given role (if the role is unlocked)")]
        public async Task JoinRole([NotNull] IRole role)
        {
            if (!await _roles.IsUnlocked(role))
            {
                await TypingReplyAsync("You cannot give yourself that role, it is not unlocked.");
                return;
            }

            if (!(Context.User is IGuildUser gu))
            {
                await TypingReplyAsync("You need to do this within a guild channel");
                return;
            }

            await gu.AddRoleAsync(role);
            await TypingReplyAsync($"Granted role `{role.Name}` to `{gu.Nickname ?? gu.Username}`");
        }

        [Command("leave"), Summary("I will remove the given role (if the role is unlocked)")]
        public async Task LeaveRole([NotNull] IRole role)
        {
            if (!await _roles.IsUnlocked(role))
            {
                await TypingReplyAsync("You cannot remove that role from yourself, it is not unlocked.");
                return;
            }

            if (!(Context.User is IGuildUser gu))
            {
                await TypingReplyAsync("You need to do this within a guild channel");
                return;
            }

            await gu.RemoveRoleAsync(role);
            await TypingReplyAsync($"Removed role `{role.Name}` from `{gu.Nickname ?? gu.Username}`");
        }

        [Command("list"), Alias("show", "unlocked"), Summary("I will list the unlocked roles")]
        public async Task ListRoles()
        {
            var roles = await _roles.GetUnlocked(Context.Guild).ToArray();
            await DisplayItemList(
                roles,
                () => "There are no unlocked roles",
                item => TypingReplyAsync($"`{item.Name}` is the only unlocked role in this guild"),
                items => $"There are {items.Count} unlocked roles in this guild:",
                (item, index) => $"`{item.Name}`"
            );
        }

        [Command("test"), Alias("query"), Summary("I will tell you if the given role is unlocked")]
        public async Task TestRole([NotNull] IRole role)
        {
            try
            {
                if (await _roles.IsUnlocked(role))
                    await TypingReplyAsync($"The role `{role.Name}` is **unlocked**");
                else
                    await TypingReplyAsync($"The role `{role.Name}` is **locked**");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [RequireOwner, Command("unlock"), Summary("I will unlock the given role (allow anyone to join/leave it)")]
        public async Task UnlockRole([NotNull] IRole role)
        {
            await _roles.Unlock(role);
            await TypingReplyAsync($"Unlocked `{role.Name}`");
        }

        [RequireOwner, Command("lock"), Summary("I will lock the given role (stop allowing anyone to join/leave it)")]
        public async Task LockRole([NotNull] IRole role)
        {
            await _roles.Lock(role);
            await TypingReplyAsync($"Locked `{role.Name}`");
        }
    }
}
