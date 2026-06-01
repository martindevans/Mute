using Discord;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
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
        var role = CreateRole(guild.Object, 10, "Role");

        Assert.IsFalse(await service.IsUnlocked(role.Object));

        await service.Unlock(role.Object);

        Assert.IsTrue(await service.IsUnlocked(role.Object));

        await service.Lock(role.Object);

        Assert.IsFalse(await service.IsUnlocked(role.Object));
    }

    [TestMethod]
    public async Task UnlockIsIdempotent()
    {
        var service = new DatabaseGroupService(new SqliteInMemoryDatabase());
        var guild = CreateGuild(1, "Guild");
        var alpha = CreateRole(guild.Object, 10, "Alpha");

        guild.Setup(g => g.GetRoleAsync(10, It.IsAny<RequestOptions>()))
             .ReturnsAsync(alpha.Object);

        await service.Unlock(alpha.Object);
        await service.Unlock(alpha.Object);

        var unlocked = (await service.GetUnlocked(guild.Object)).ToArray();

        CollectionAssert.AreEqual(new[] { alpha.Object }, unlocked);
    }

    [TestMethod]
    public async Task GetUnlockedReturnsSortedRolesForGuild()
    {
        var service = new DatabaseGroupService(new SqliteInMemoryDatabase());
        var guild = CreateGuild(1, "Guild");
        var otherGuild = CreateGuild(2, "Other Guild");
        var zebra = CreateRole(guild.Object, 10, "Zebra");
        var alpha = CreateRole(guild.Object, 11, "Alpha");
        var other = CreateRole(otherGuild.Object, 12, "Other");

        guild.Setup(g => g.GetRoleAsync(10, It.IsAny<RequestOptions>()))
             .ReturnsAsync(zebra.Object);
        guild.Setup(g => g.GetRoleAsync(11, It.IsAny<RequestOptions>()))
             .ReturnsAsync(alpha.Object);
        guild.Setup(g => g.GetRoleAsync(12, It.IsAny<RequestOptions>()))
             .ThrowsAsync(new AssertFailedException("Should not fetch roles from another guild"));

        await service.Unlock(zebra.Object);
        await service.Unlock(alpha.Object);
        await service.Unlock(other.Object);

        var unlocked = (await service.GetUnlocked(guild.Object)).ToArray();

        CollectionAssert.AreEqual(new[] { alpha.Object, zebra.Object }, unlocked);
    }

    [TestMethod]
    public async Task GetUnlockedSkipsRolesThatCannotBeFetched()
    {
        var service = new DatabaseGroupService(new SqliteInMemoryDatabase());
        var guild = CreateGuild(1, "Guild");
        var valid = CreateRole(guild.Object, 10, "Alpha");
        var missing = CreateRole(guild.Object, 11, "Missing");

        guild.Setup(g => g.GetRoleAsync(10, It.IsAny<RequestOptions>()))
             .ReturnsAsync(valid.Object);
        guild.Setup(g => g.GetRoleAsync(11, It.IsAny<RequestOptions>()))
             .ThrowsAsync(new InvalidOperationException("Missing role"));

        await service.Unlock(valid.Object);
        await service.Unlock(missing.Object);

        var unlocked = (await service.GetUnlocked(guild.Object)).ToArray();

        CollectionAssert.AreEqual(new[] { valid.Object }, unlocked);
    }

    private static Mock<IGuild> CreateGuild(ulong id, string name)
    {
        var guild = new Mock<IGuild>();
        guild.SetupGet(g => g.Id).Returns(id);
        guild.SetupGet(g => g.Name).Returns(name);
        return guild;
    }

    private static Mock<IRole> CreateRole(IGuild guild, ulong id, string name)
    {
        var role = new Mock<IRole>();
        role.SetupGet(r => r.Guild).Returns(guild);
        role.SetupGet(r => r.Id).Returns(id);
        role.SetupGet(r => r.Name).Returns(name);
        return role;
    }
}
