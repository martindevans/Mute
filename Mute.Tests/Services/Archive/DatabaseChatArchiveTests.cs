#nullable enable

using System;
using System.Threading.Tasks;
using Dapper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mute.Moe.Services.Archive;
using Mute.Moe.Services.Database;

namespace Mute.Tests.Services.Archive;

[TestClass]
public class DatabaseChatArchiveTests
{
    private sealed record Row(string Context, string Channel, string MessageId, string Instant, string Content, string? Mention);

    private static async Task<Row?> GetRow(IDatabaseService db, ulong messageId)
    {
        using var conn = db.GetConnection();
        return await conn.QueryFirstOrDefaultAsync<Row>(
            "SELECT `Context`, `Channel`, `MessageId`, `Instant`, `Content`, `Mention` FROM `ArchiveMessages` WHERE `MessageId` = @MessageId",
            new { MessageId = messageId.ToString() }
        );
    }

    private static int Count(IDatabaseService db)
    {
        using var conn = db.GetConnection();
        return conn.ExecuteScalar<int>("SELECT COUNT(*) FROM `ArchiveMessages`");
    }

    [TestMethod]
    public async Task Insert_ReturnsTrue_WhenMessageIsNew()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        var inserted = await archive.Insert(1, 2, 3, new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero), "hello", null);

        Assert.IsTrue(inserted);
    }

    [TestMethod]
    public async Task Insert_ReturnsFalse_WhenMessageIdAlreadyExists()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        var instant = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var inserted = await archive.Insert(1, 2, 3, instant, "hello", null);
        var duplicate = await archive.Insert(1, 2, 3, instant, "hello", null);

        Assert.IsTrue(inserted);
        Assert.IsFalse(duplicate);
    }

    [TestMethod]
    public async Task Insert_Duplicate_KeepsOriginalRow()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        var instant = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        await archive.Insert(1, 2, 3, instant, "original", null);
        await archive.Insert(1, 2, 3, instant, "replaced", null);

        Assert.AreEqual(1, Count(db));
        var row = await GetRow(db, 3);
        Assert.IsNotNull(row);
        Assert.AreEqual("original", row.Content);
    }

    [TestMethod]
    public async Task Insert_StoresAllFields()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        var instant = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        await archive.Insert(111, 222, 333, instant, "content", 444);

        var row = await GetRow(db, 333);

        Assert.IsNotNull(row);
        Assert.AreEqual("111", row.Context);
        Assert.AreEqual("222", row.Channel);
        Assert.AreEqual("333", row.MessageId);
        Assert.IsTrue(row.Instant.StartsWith("2026-01-02 03:04:05"), $"Unexpected instant format: {row.Instant}");
        Assert.AreEqual("content", row.Content);
        Assert.AreEqual("444", row.Mention);
    }

    [TestMethod]
    public async Task Insert_NullMention_StoresNull()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        var instant = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        await archive.Insert(1, 2, 3, instant, "content", null);

        var row = await GetRow(db, 3);

        Assert.IsNotNull(row);
        Assert.IsNull(row.Mention);
    }

    [TestMethod]
    public async Task Insert_MultipleMessages_AllStored()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        var instant = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        for (var i = 0; i < 3; i++)
            await archive.Insert(1, 2, (ulong)(100 + i), instant, $"msg {i}", null);

        Assert.AreEqual(3, Count(db));

        for (var i = 0; i < 3; i++)
        {
            var row = await GetRow(db, (ulong)(100 + i));
            Assert.IsNotNull(row);
            Assert.AreEqual($"msg {i}", row.Content);
        }
    }

    [TestMethod]
    public async Task Insert_DifferentContexts_Independent()
    {
        var db = new SqliteInMemoryDatabase();
        var archive = new DatabaseChatArchive(db);

        var instant = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        await archive.Insert(1, 2, 3, instant, "context one", null);
        await archive.Insert(4, 2, 3, instant, "context two", null);

        Assert.AreEqual(1, Count(db));

        var row = await GetRow(db, 3);
        Assert.IsNotNull(row);
        Assert.AreEqual("1", row.Context);
        Assert.AreEqual("context one", row.Content);
    }
}
