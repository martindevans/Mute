using Dapper;
using Mute.Moe.Services.Database;
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
        connection.Execute("CREATE TABLE IF NOT EXISTS `ArchiveMessages` (" +
                           "    `Context` TEXT NOT NULL," +
                           "    `Channel` TEXT NOT NULL," +
                           "    `MessageId` TEXT PRIMARY KEY," +
                           "    `Sender` TEXT NOT NULL," +
                           "    `Instant` TEXT NOT NULL," +
                           "    `Content` TEXT NOT NULL," +
                           "    `Mention` TEXT" +
                           ")");
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
}