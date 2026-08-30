#nullable enable

using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Archive;
using Mute.Moe.Services.Database;

namespace Mute.Tests.Services.Archive;

[TestClass]
public class DatabaseChatArchiveTests
{
    private static Task Insert(DatabaseChatArchive archive, ulong context, ulong channel, ulong messageId, ulong senderId)
    {
        return archive.Insert(context, channel, messageId, senderId, DateTimeOffset.UnixEpoch, "test", null);
    }

    [TestMethod]
    public void Count_ReturnsZero_WhenNoMessages()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        Assert.AreEqual(0, archive.Count(111));
    }

    [TestMethod]
    public async Task Count_ReturnsAllMessages_WithoutFilters()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        await Insert(archive, 111, 1, 1, 1);
        await Insert(archive, 111, 2, 2, 2);
        await Insert(archive, 111, 1, 3, 3);

        Assert.AreEqual(3, archive.Count(111));
    }

    [TestMethod]
    public async Task Count_OnlyCountsMessagesInContext()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        await Insert(archive, 111, 1, 1, 1);
        await Insert(archive, 222, 1, 2, 2);

        Assert.AreEqual(1, archive.Count(111));
        Assert.AreEqual(1, archive.Count(222));
        Assert.AreEqual(0, archive.Count(333));
    }

    [TestMethod]
    public async Task Count_ReturnsOnlyMessagesForChannel_WithChannelFilter()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        await Insert(archive, 111, 1, 1, 1);
        await Insert(archive, 111, 1, 2, 2);
        await Insert(archive, 111, 2, 3, 3);

        Assert.AreEqual(2, archive.Count(111, 1));
        Assert.AreEqual(1, archive.Count(111, 2));
        Assert.AreEqual(0, archive.Count(111, 3));
    }

    [TestMethod]
    public async Task Count_ReturnsOnlyMessagesForSender_WithSenderFilter()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        await Insert(archive, 111, 1, 1, 1);
        await Insert(archive, 111, 1, 2, 1);
        await Insert(archive, 111, 1, 3, 2);

        Assert.AreEqual(2, archive.Count(111, senderId: 1));
        Assert.AreEqual(1, archive.Count(111, senderId: 2));
        Assert.AreEqual(0, archive.Count(111, senderId: 3));
    }

    [TestMethod]
    public async Task Count_CombinesChannelAndSenderFilters()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        await Insert(archive, 111, 1, 1, 1);
        await Insert(archive, 111, 1, 2, 2);
        await Insert(archive, 111, 2, 3, 1);

        Assert.AreEqual(1, archive.Count(111, 1, 1));
        Assert.AreEqual(1, archive.Count(111, 1, 2));
        Assert.AreEqual(0, archive.Count(111, 2, 2));
    }
}
