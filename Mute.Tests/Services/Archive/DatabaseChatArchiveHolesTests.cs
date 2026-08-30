#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Archive;
using Mute.Moe.Services.Database;

namespace Mute.Tests.Services.Archive;

[TestClass]
public class DatabaseChatArchiveHolesTests
{
    private sealed record Row(long Id, string ChannelId, string StartMessageId, long Forward);

    private static int Count(IDatabaseService db)
    {
        using var conn = db.GetConnection();
        return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM `ArchiveHoles`");
    }

    private static async Task<List<Row>> GetRows(IDatabaseService db)
    {
        using var conn = db.GetConnection();
        var rows = await conn.QueryAsync<Row>(
            "SELECT `rowid` as Id, `ChannelId`, `StartMessageId`, `Forward` FROM `ArchiveHoles`"
        );
        return [.. rows];
    }

    [TestMethod]
    public async Task Create_StoresHole()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 222, true);

        Assert.AreEqual(1, Count(db));
        var row = (await GetRows(db)).Single();
        Assert.AreEqual("111", row.ChannelId);
        Assert.AreEqual("222", row.StartMessageId);
        Assert.AreEqual(1, row.Forward);
    }

    [TestMethod]
    public async Task Create_Duplicate_IsIgnored()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 222, true);
        await holes.Create(111, 222, true);

        Assert.AreEqual(1, Count(db));
    }

    [TestMethod]
    public async Task Create_DifferentDirection_StoresSeparateHoles()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 222, true);
        await holes.Create(111, 222, false);

        Assert.AreEqual(2, Count(db));
    }

    [TestMethod]
    public async Task Create_DifferentChannels_StoreSeparateHoles()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 222, true);
        await holes.Create(333, 222, true);

        Assert.AreEqual(2, Count(db));
    }

    [TestMethod]
    public async Task Read_ReturnsNull_WhenNoHoles()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        var result = await holes.Read();

        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task Read_ReturnsStoredHole()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 222, false);
        var result = await holes.Read();

        Assert.IsNotNull(result);
        Assert.AreEqual(111ul, result.ChannelId);
        Assert.AreEqual(222ul, result.StartMessageId);
        Assert.IsFalse(result.Forward);
    }

    [TestMethod]
    public async Task Read_ReturnsOneOfTheHoles()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 222, true);
        await holes.Create(333, 444, false);
        var result = await holes.Read();

        Assert.IsNotNull(result);
        var matches = new[]
            {
                (result.ChannelId == 111 && result.StartMessageId == 222 && result.Forward),
                (result.ChannelId == 333 && result.StartMessageId == 444 && !result.Forward)
            };
        Assert.IsTrue(matches.Any(), $"Read returned an unexpected hole: {result}");
    }

    [TestMethod]
    public async Task Delete_RemovesHole()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 222, true);
        var hole = await holes.Read();
        Assert.IsNotNull(hole);

        await holes.Delete(hole.Id);

        Assert.AreEqual(0, Count(db));
        Assert.IsNull(await holes.Read());
    }

    [TestMethod]
    public async Task Delete_LeavesOtherHolesIntact()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 222, true);
        await holes.Create(333, 444, false);

        var toDelete = (await GetRows(db)).Single(r => r.ChannelId == "111");
        await holes.Delete(toDelete.Id);

        Assert.AreEqual(1, Count(db));
        var remaining = (await GetRows(db)).Single();
        Assert.AreEqual("333", remaining.ChannelId);
        Assert.AreEqual("444", remaining.StartMessageId);
    }

    [TestMethod]
    public void Count_ReturnsZero_WhenNoHoles()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        Assert.AreEqual(0, holes.Count(null));
        Assert.AreEqual(0, holes.Count(111));
    }

    [TestMethod]
    public async Task Count_ReturnsAllHoles_WithoutChannelFilter()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 1, true);
        await holes.Create(111, 2, true);
        await holes.Create(333, 3, true);

        Assert.AreEqual(3, holes.Count(null));
    }

    [TestMethod]
    public async Task Count_ReturnsOnlyHolesForChannel_WithChannelFilter()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 1, true);
        await holes.Create(111, 2, true);
        await holes.Create(333, 3, true);

        Assert.AreEqual(3, holes.Count(null));
        Assert.AreEqual(2, holes.Count(111));
        Assert.AreEqual(1, holes.Count(333));
    }

    [TestMethod]
    public async Task Count_ReturnsZero_ForUnknownChannel()
    {
        var db = new SqliteInMemoryDatabase();
        var holes = new DatabaseChatArchiveHoles(db);

        await holes.Create(111, 1, true);

        Assert.AreEqual(0, holes.Count(999));
    }
}
