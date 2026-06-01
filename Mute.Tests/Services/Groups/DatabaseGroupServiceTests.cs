using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.Groups;

namespace Mute.Tests.Services.Groups;

[TestClass]
public class DatabaseGroupServiceTests
{
    [TestMethod]
    public async Task UnlockAndLockChangesUnlockedState()
    {
        var service = new DatabaseGroupService(new SqliteInMemoryDatabase());
        var guild = CreateGuild(1, "Guild");
        var role = CreateRole(guild, 10, "Role");

        Assert.IsFalse(await service.IsUnlocked(role));

        await service.Unlock(role);

        Assert.IsTrue(await service.IsUnlocked(role));

        await service.Lock(role);

        Assert.IsFalse(await service.IsUnlocked(role));
    }

    [TestMethod]
    public async Task UnlockIsIdempotent()
    {
        var service = new DatabaseGroupService(new SqliteInMemoryDatabase());
        var guild = CreateGuild(1, "Guild");
        var alpha = CreateRole(guild, 10, "Alpha");
        SetRoleResult(guild, 10, alpha);

        await service.Unlock(alpha);
        await service.Unlock(alpha);

        var unlocked = (await service.GetUnlocked(guild)).ToArray();

        CollectionAssert.AreEqual(new[] { alpha }, unlocked);
    }

    [TestMethod]
    public async Task GetUnlockedReturnsSortedRolesForGuild()
    {
        var service = new DatabaseGroupService(new SqliteInMemoryDatabase());
        var guild = CreateGuild(1, "Guild");
        var otherGuild = CreateGuild(2, "Other Guild");
        var zebra = CreateRole(guild, 10, "Zebra");
        var alpha = CreateRole(guild, 11, "Alpha");
        var other = CreateRole(otherGuild, 12, "Other");

        SetRoleResult(guild, 10, zebra);
        SetRoleResult(guild, 11, alpha);
        SetRoleException(guild, 12, new AssertFailedException("Should not fetch roles from another guild"));

        await service.Unlock(zebra);
        await service.Unlock(alpha);
        await service.Unlock(other);

        var unlocked = (await service.GetUnlocked(guild)).ToArray();

        CollectionAssert.AreEqual(new[] { alpha, zebra }, unlocked);
    }

    [TestMethod]
    public async Task GetUnlockedSkipsRolesThatCannotBeFetched()
    {
        var service = new DatabaseGroupService(new SqliteInMemoryDatabase());
        var guild = CreateGuild(1, "Guild");
        var valid = CreateRole(guild, 10, "Alpha");
        var missing = CreateRole(guild, 11, "Missing");

        SetRoleResult(guild, 10, valid);
        SetRoleException(guild, 11, new InvalidOperationException("Missing role"));

        await service.Unlock(valid);
        await service.Unlock(missing);

        var unlocked = (await service.GetUnlocked(guild)).ToArray();

        CollectionAssert.AreEqual(new[] { valid }, unlocked);
    }

    private static IGuild CreateGuild(ulong id, string name)
    {
        var guild = DispatchProxy.Create<IGuild, InterfaceProxy<IGuild>>();
        var proxy = (InterfaceProxy<IGuild>)(object)guild;
        proxy.Handler = (method, args) => method.Name switch
        {
            "get_Id" => id,
            "get_Name" => name,
            "GetRoleAsync" => GetRole(proxy.RoleResults, args),
            _ => throw new NotImplementedException(method.Name)
        };

        return guild;
    }

    private static IRole CreateRole(IGuild guild, ulong id, string name)
    {
        return CreateProxy<IRole>((method, _) => method.Name switch
        {
            "get_Guild" => guild,
            "get_Id" => id,
            "get_Name" => name,
            _ => throw new NotImplementedException(method.Name)
        });
    }

    private static void SetRoleResult(IGuild guild, ulong id, IRole role)
    {
        var roles = GetRoleStore(guild);
        roles[id] = () => Task.FromResult(role);
    }

    private static void SetRoleException(IGuild guild, ulong id, Exception exception)
    {
        var roles = GetRoleStore(guild);
        roles[id] = () => Task.FromException<IRole>(exception);
    }

    private static Task<IRole> GetRole(Dictionary<ulong, Func<Task<IRole>>> roles, object?[]? args)
    {
        var id = (ulong)args![0]!;
        if (roles.TryGetValue(id, out var getter))
            return getter();

        throw new KeyNotFoundException($"No role configured for {id}");
    }

    private static Dictionary<ulong, Func<Task<IRole>>> GetRoleStore(IGuild guild)
        => ((InterfaceProxy<IGuild>)(object)guild).RoleResults;

    private static T CreateProxy<T>(Func<MethodInfo, object?[]?, object?> handler)
        where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceProxy<T>>();
        ((InterfaceProxy<T>)(object)proxy).Handler = handler;
        return proxy;
    }

    private sealed class InterfaceProxy<T>
        : DispatchProxy
        where T : class
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = default!;

        public Dictionary<ulong, Func<Task<IRole>>> RoleResults { get; } = new();

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
            => Handler(targetMethod ?? throw new ArgumentNullException(nameof(targetMethod)), args);
    }
}
