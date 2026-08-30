using Dapper;
using Mute.Moe.Services.Database;
using System.Threading.Tasks;

namespace Mute.Moe.Services.Archive;

/// <summary>
/// Represents a service for managing "holes" in the chat archive, which are gaps in the message history
/// that may need to be filled. This implementation uses a database to persist and manage these archive holes.
/// </summary>
public class DatabaseChatArchiveHoles
    : IChatArchiveHoles
{
    private readonly IDatabaseService _database;

    /// <summary>
    /// Initializes a new instance of the <see cref="DatabaseChatArchiveHoles"/> class.
    /// </summary>
    /// <param name="database">The database service used to manage the chat archive holes.</param>
    public DatabaseChatArchiveHoles(IDatabaseService database)
    {
        _database = database;

        // Create database structure
        using var connection = _database.GetConnection();
        connection.Execute("""
                           CREATE TABLE IF NOT EXISTS `ArchiveHoles` (
                               `ChannelId` TEXT NOT NULL,
                               `StartMessageId` TEXT NOT NULL,
                               `Forward` INTEGER NOT NULL,
                               PRIMARY KEY(ChannelId, StartMessageId, Forward)
                           )
                           """);
    }

    /// <inheritdoc />
    public async Task Create(ulong channel, ulong startMessage, bool forward)
    {
        using var connection = _database.GetConnection();
        
        await connection.ExecuteAsync(
            "INSERT OR IGNORE INTO `ArchiveHoles` (`StartMessageId`, `ChannelId`, `Forward`) VALUES (@StartMessageId, @ChannelId, @Forward)",
            new
            {
                StartMessageId = startMessage.ToString(),
                ChannelId = channel,
                Forward = forward ? 1 : 0
            }
        );
    }

    /// <inheritdoc />
    public async Task<ChatArchiveHole?> Read()
    {
        using var connection = _database.GetConnection();

        var result = await connection.QueryFirstOrDefaultAsync<ArchiveHole>("SELECT `rowid` as Id, `ChannelId`, `StartMessageId`, `Forward` FROM `ArchiveHoles` ORDER BY RANDOM() LIMIT 1");

        return result == null
             ? null
             : new ChatArchiveHole(result.Id, ulong.Parse(result.ChannelId), ulong.Parse(result.StartMessageId), Convert.ToBoolean(result.Forward));
    }

    /// <inheritdoc />
    public async Task Delete(long id)
    {
        using var connection = _database.GetConnection();
        await connection.ExecuteAsync(
            "DELETE FROM `ArchiveHoles` WHERE `rowid` = @Id",
            new
            {
                Id = id
            }
        );
    }

    /// <inheritdoc />
    public int Count(ulong? channel)
    {
        using var connection = _database.GetConnection();

        return channel is null
            ? connection.ExecuteScalar<int>("SELECT COUNT(*) FROM `ArchiveHoles`")
            : connection.ExecuteScalar<int>(
                "SELECT COUNT(*) FROM `ArchiveHoles` WHERE `ChannelId` = @ChannelId",
                new { ChannelId = channel.Value.ToString() }
              );
    }

    private record ArchiveHole(long Id, string ChannelId, string StartMessageId, long Forward);
}