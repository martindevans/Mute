using Dapper;
using Mute.Moe.Services.Database;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Archive;

/// <summary>
/// Stores previous chat messages
/// </summary>
public class DatabaseChatArchive
    : IChatArchive
{
    private const string InsertArchiveMessageSql = "INSERT OR IGNORE INTO `ArchiveMessages` (Context, Channel, MessageId, Sender, Instant, Content, Mention)" +
                                                   "values(@Context, @Channel, @MessageId, @Sender, @Instant, @Content, @Mention)";

    private readonly IDatabaseService _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseChatArchive"/> class.
    /// </summary>
    /// <param name="database">The database service used to manage the storage of chat messages.</param>
    public DatabaseChatArchive(IDatabaseService database)
    {
        _database = database;

        // Create database structure
        using var connection = _database.GetConnection();
        connection.Execute(
            """
            CREATE TABLE IF NOT EXISTS `ArchiveMessages` (
                `Context` TEXT NOT NULL,
                `Channel` TEXT NOT NULL,
                `MessageId` TEXT PRIMARY KEY,
                `Sender` TEXT NOT NULL,
                `Instant` TEXT NOT NULL,
                `Content` TEXT NOT NULL,
                `Mention` TEXT
            );
            """
        );

        // Create FTS table
        SetupArchiveMessagesFts(connection);
    }

    private static void SetupArchiveMessagesFts(IDbConnection connection)
    {
        // Check if this is first time creation of the archive
        var rebuildRequired = connection.ExecuteScalar<long>("SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='ArchiveMessages_fts'") > 0;

        // Create the FTS5 virtual table if it doesn't exist
        connection.Execute(
            """
            CREATE VIRTUAL TABLE IF NOT EXISTS ArchiveMessages_fts USING fts5(
               Content,
               content='ArchiveMessages',
               content_rowid='rowid'
            );
            """
        );

        // Create the triggers if they don't exist (IF NOT EXISTS makes this idempotent)
        connection.Execute(
            """
            CREATE TRIGGER IF NOT EXISTS ArchiveMessages_ai
            AFTER INSERT ON ArchiveMessages BEGIN
               INSERT INTO ArchiveMessages_fts(rowid, Content)
               VALUES (new.rowid, new.Content);
            END;
            """
        );

        connection.Execute(
            """
            CREATE TRIGGER IF NOT EXISTS ArchiveMessages_ad
            AFTER DELETE ON ArchiveMessages BEGIN
               INSERT INTO ArchiveMessages_fts(ArchiveMessages_fts, rowid, Content)
               VALUES('delete', old.rowid, old.Content);
            END;
            """
        );

        connection.Execute(
            """
            CREATE TRIGGER IF NOT EXISTS ArchiveMessages_au
            AFTER UPDATE ON ArchiveMessages BEGIN
               INSERT INTO ArchiveMessages_fts(ArchiveMessages_fts, rowid, Content)
               VALUES('delete', old.rowid, old.Content);
               INSERT INTO ArchiveMessages_fts(rowid, Content)
               VALUES (new.rowid, new.Content);
            END;

            """
        );

        // Rebuild archive to keep in sync
        if (rebuildRequired)
        {
            connection.Execute(
                """
                INSERT INTO ArchiveMessages_fts(ArchiveMessages_fts) VALUES('rebuild');
                """
            );
        }
    }

    /// <inheritdoc />
    public async Task<bool> Insert(ulong context, ulong channel, ulong messageId, ulong senderId, DateTimeOffset instant, string content, ulong? mention)
    {
        using var connection = _database.GetConnection();

        var rows = await connection.ExecuteAsync(
            InsertArchiveMessageSql,
            new
            {
                Context = context.ToString(),
                Channel = channel.ToString(),
                MessageId = messageId.ToString(),
                Sender = senderId.ToString(),
                Instant = instant,
                Content = content,
                Mention = mention?.ToString()
            }
        );

        return rows > 0;
    }

    /// <inheritdoc />
    public int Count(ulong context, ulong? channel = null, ulong? senderId = null)
    {
        using var connection = _database.GetConnection();

        return connection.ExecuteScalar<int>(
            "SELECT COUNT(*) FROM `ArchiveMessages` " +
            "WHERE `Context` = @Context " +
            "AND (`Channel` = @Channel OR @Channel IS NULL) " +
            "AND (`Sender` = @SenderId OR @SenderId IS NULL)",
            new
            {
                Context = context.ToString(),
                Channel = channel?.ToString(),
                SenderId = senderId?.ToString()
            }
        );
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArchiveFtsSearchResult>> Search(string query, int limit = 25)
    {
        if (string.IsNullOrWhiteSpace(query))
            return [];

        // Treat input as a literal phrase; escape any embedded quotes.
        var ftsQuery = "\"" + query.Replace("\"", "\"\"") + "\"";

        using var connection = _database.GetConnection();
        var rows = connection.Query<RawHit>(
            """
                SELECT am.MessageId                                     AS MessageId,
                       highlight(ArchiveMessages_fts, 0, '<b>', '</b>') AS Snippet,
                       bm25(ArchiveMessages_fts)                        AS Rank
                FROM ArchiveMessages am
                JOIN ArchiveMessages_fts fts ON am.rowid = fts.rowid
                WHERE ArchiveMessages_fts MATCH @query
                ORDER BY Rank
                LIMIT @limit
            """,
            new { query = ftsQuery, limit }
        );

        return rows
              .Select(r => new ArchiveFtsSearchResult(
                   ulong.Parse(r.MessageId, CultureInfo.InvariantCulture),
                   r.Snippet ?? string.Empty,
                   (float)r.Rank)
               )
              .ToList();
    }

    private sealed class RawHit
    {
        public string MessageId { get; set; } = string.Empty;
        public string? Snippet { get; set; }
        public double Rank { get; set; }
    }




}