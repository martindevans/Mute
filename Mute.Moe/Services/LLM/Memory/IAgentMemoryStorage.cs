using Dapper;
using Dapper.Contrib.Extensions;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.LLM.Embedding;
using Serilog;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Mute.Moe.Services.LLM.Memory;

/// <summary>
/// Provides memory for LLM agents
/// </summary>
public interface IAgentMemoryStorage
{
    /// <summary>
    /// Store a new memory
    /// </summary>
    /// <param name="context"></param>
    /// <param name="text"></param>
    /// <param name="confidenceLogit"></param>
    /// <param name="tsx"></param>
    /// <returns></returns>
    public Task<int?> CreateMemory(ulong context, string text, float confidenceLogit, IDbTransaction tsx);

    /// <summary>
    /// Create an evidence object, return the ID
    /// </summary>
    /// <param name="context"></param>
    /// <param name="text"></param>
    /// <param name="tsx"></param>
    /// <returns></returns>
    public Task<int> CreateEvidence(ulong context, string text, IDbTransaction tsx);

    /// <summary>
    /// Create a link between a memory and some evidence
    /// </summary>
    /// <param name="evidence"></param>
    /// <param name="memory"></param>
    /// <param name="tsx"></param>
    /// <returns></returns>
    public Task CreateEvidenceLink(int evidence, int memory, IDbTransaction tsx);

    /// <summary>
    /// Retrieve a specific memory by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<AgentMemory?> Get(int id);

    /// <summary>
    /// Get all memories with the given context
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public Task<IEnumerable<AgentMemory>> Get(ulong context);

    /// <summary>
    /// Find memories with similar embedding to the query
    /// </summary>
    /// <param name="context"></param>
    /// <param name="query"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    public Task<IEnumerable<MemorySearchResult>> FindSimilar(ulong context, string query, int limit);

    /// <summary>
    /// Add a value to all confidence logits within a range
    /// </summary>
    /// <param name="minLogit">Logits below this threshold will NOT be updated</param>
    /// <param name="maxLogit">Logits above this threshold will NOT be updated</param>
    /// <param name="value">Value to add directly to logit</param>
    /// <returns></returns>
    public Task<int> AddToConfidenceLogits(float? minLogit, float? maxLogit, float value);

    /// <summary>
    /// Perform DB maintanence, deleting things:
    /// - Memories with no evidence
    /// - Evidence links referencing non-existant things
    /// - Evidence with no linked memories
    /// </summary>
    /// <returns></returns>
    public Task<int> CleanupMemoryReferences(CancellationToken cancellation);

    /// <summary>
    /// Delete a memory by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<bool> Delete(int id);
        
    /// <summary>
    /// Set the memory acess time to now
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task UpdateAccessTime(int id);
}

/// <summary>
/// Result from searching for memories
/// </summary>
/// <param name="Memory">The memory item</param>
/// <param name="Distance">Distance to query</param>
public record struct MemorySearchResult(AgentMemory Memory, float Distance);

/// <summary>
/// Stores agent memories in the database
/// </summary>
public class DatabaseAgentMemoryStorage
    : IAgentMemoryStorage
{
    private readonly IDatabaseService _database;
    private readonly IEmbeddings _embeddings;

    /// <summary>
    /// Create a new <see cref="DatabaseAgentMemoryStorage"/>
    /// </summary>
    /// <param name="database"></param>
    /// <param name="embeddings"></param>
    public DatabaseAgentMemoryStorage(IDatabaseService database, IEmbeddings embeddings)
    {
        _database = database;
        _embeddings = embeddings;

        using var connection = _database.GetConnection();

        connection.Execute("""
                   CREATE TABLE IF NOT EXISTS `AgentMemorys`
                   (
                       `ID`              INTEGER PRIMARY KEY ASC,
                       `Context`         INTEGER,
                       `Text`            TEXT NOT NULL,
                       `Embedding`       BLOB,
                       `EmbeddingModel`  TEXT NOT NULL,
                       `ConfidenceLogit` REAL,
                       `CreationUnix`    INTEGER,
                       `AccessUnix`      INTEGER
                   );
                   """);

        connection.Execute("""
                           CREATE TABLE IF NOT EXISTS `AgentMemoryEvidences`
                           (
                               `ID`              INTEGER PRIMARY KEY ASC,
                               `Context`         INTEGER,
                               `Text`            TEXT NOT NULL,
                               `CreationUnix`    INTEGER,
                               `AccessUnix`      INTEGER
                           );
                           """);

        connection.Execute("""
                           CREATE TABLE IF NOT EXISTS `AgentMemoryEvidenceLinks`
                           (
                               `EvidenceId`      INTEGER,
                               `MemoryId`        INTEGER,
                               FOREIGN KEY(EvidenceId) REFERENCES AgentMemoryEvidences(ID),
                               FOREIGN KEY(MemoryId)   REFERENCES AgentMemorys(ID)
                           );
                           """);

        // Initialise the column as a vector store
        connection.Execute($"SELECT vector_init('AgentMemorys', 'Embedding', 'type=FLOAT32,dimension={_embeddings.Dimensions},distance=cosine');");
        connection.Execute("SELECT vector_quantize('AgentMemorys', 'Embedding')");

        // Add indices
        connection.Execute("CREATE INDEX IF NOT EXISTS `AgentMemorysByContext` ON `AgentMemorys` (`Context` ASC);");
        connection.Execute("CREATE INDEX IF NOT EXISTS `AgentMemorysByConfidence` ON `AgentMemorys` (`ConfidenceLogit` ASC);");
        connection.Execute("CREATE INDEX IF NOT EXISTS `AgentMemoryEvidenceLinksByMemoryId` ON AgentMemoryEvidenceLinks(MemoryId);");
        connection.Execute("CREATE INDEX IF NOT EXISTS `AgentMemoryEvidenceLinksOnEvidenceId` ON AgentMemoryEvidenceLinks(EvidenceId);");

        // Check for incorrect embeddings
        var count = connection.ExecuteScalar<int>(
            "SELECT Count(*) FROM `AgentMemorys` WHERE (EmbeddingModel != @EmbeddingModel)",
            new
            {
                EmbeddingModel = _embeddings.Model
            }
        );

        // Launch task to fix embeddings
        if (count > 0)
        {
            Log.Warning("Detected {0} memories with incorrect embedding model", count);
            Task.Run(UpdateMemoryEmbeddings);
        }
    }

    private async Task UpdateMemoryEmbeddings()
    {
        Log.Information("Begin UpdateMemoryEmbeddings");

        while (true)
        {
            using var connection = _database.GetConnection();

            // Get a batch of items
            var items = (await connection.QueryAsync<AgentMemory>("SELECT * FROM AgentMemorys WHERE (EmbeddingModel != @EmbeddingModel) LIMIT 8")).ToList();
            Log.Information("UpdateMemoryEmbeddings Batch={0}", items.Count);
            if (items.Count == 0)
                return;

            // Get all the texts
            var texts = items.Select(a => a.Text).ToArray();

            // embed texts in one batch
            var embeds = (await _embeddings.Embed(texts))?.ToDictionary(a => a.Input, a => a.Result);
            if (embeds == null)
                return;

            // Update items
            foreach (var agentMemory in items)
            {
                if (!embeds.TryGetValue(agentMemory.Text, out var result))
                    continue;

                await connection.ExecuteAsync(
                    """
                    UPDATE AgentMemorys
                    SET `Embedding` = @Embedding
                    SET `EmbeddingModel` = @EmbeddingModel
                    WHERE `ID` = @ID
                    """,
                    new
                    {
                        ID = agentMemory.ID,
                        Embedding = MemoryMarshal.Cast<float, byte>(result.Span).ToArray(),
                        EmbeddingModel = _embeddings.Model
                    }
                );

                await Task.Delay(1);
            }

            // Longer delay between batches
            await Task.Delay(25);
        }
    }

    /// <inheritdoc />
    public async Task<int?> CreateMemory(ulong context, string text, float confidenceLogit, IDbTransaction tsx)
    {
        var embedding = await _embeddings.Embed(text);
        if (embedding == null)
            return null;

        var now = DateTime.UtcNow.UnixTimestamp();
        var memory = new AgentMemory
        {
            Context = context,
            Text = text,
            Embedding = MemoryMarshal.Cast<float, byte>(embedding.Result.Span).ToArray(),
            EmbeddingModel = _embeddings.Model,
            ConfidenceLogit = confidenceLogit,
            CreationUnix = now,
            AccessUnix = now,
        };

        return await tsx.Connection.InsertAsync(memory, tsx);
    }

    /// <inheritdoc />
    public async Task<int> CreateEvidence(ulong context, string text, IDbTransaction tsx)
    {
        var now = DateTime.UtcNow.UnixTimestamp();
        var evidence = new AgentMemoryEvidence
        {
            Context = context,
            Text = text,
            CreationUnix = now,
            AccessUnix = now,
        };

        return await tsx.Connection.InsertAsync(evidence, tsx);
    }

    /// <inheritdoc />
    public async Task CreateEvidenceLink(int evidence, int memory, IDbTransaction tsx)
    {
        var link = new AgentMemoryEvidenceLink
        {
            EvidenceId = evidence,
            MemoryId = memory
        };

        await tsx.Connection.InsertAsync(link, tsx);
    }

    /// <inheritdoc />
    public async Task<AgentMemory?> Get(int id)
    {
        using var connection = _database.GetConnection();
        return connection.QuerySingleOrDefault<AgentMemory>(
            "SELECT * FROM AgentMemorys WHERE ID = @id",
            new { id = id }
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentMemory>> Get(ulong context)
    {
        const string SQL = """
                           SELECT *
                           FROM AgentMemorys
                           WHERE Context = @Context
                           """;

        using var connection = _database.GetConnection();
        return await connection.QueryAsync<AgentMemory>(
            SQL,
            new
            {
                Context = context
            }
        );
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MemorySearchResult>> FindSimilar(ulong context, string query, int limit)
    {
        var queryEmbedding = await _embeddings.Embed(query);
        if (queryEmbedding == null)
        {
            Log.Warning("Failed to embed memory query string");
            return [ ];
        }

        const string SQL = """
                           SELECT t.*, v.distance
                           FROM AgentMemorys as t
                           JOIN vector_quantize_scan_stream(
                               'AgentMemorys',
                               'Embedding',
                               @QueryEmbedding
                           ) AS v
                           ON t.rowid = v.rowid
                           WHERE Context = @Context
                           AND EmbeddingModel = @EmbeddingModel
                           ORDER BY v.distance ASC
                           LIMIT @TopK;
                           """;

        using var connection = _database.GetConnection();
        var result = connection.Query<AgentMemory, double, MemorySearchResult>(
            SQL,
            (m, d) => new(m, (float)d),
            new
            {
                Context = context,
                QueryEmbedding = MemoryMarshal.Cast<float, byte>(queryEmbedding.Result.Span).ToArray(),
                TopK = limit,
                EmbeddingModel = queryEmbedding.Model
            },
            splitOn: "distance"
        );

        return result;
    }

    /// <inheritdoc />
    public async Task<int> AddToConfidenceLogits(float? minLogit, float? maxLogit, float value)
    {
        if (minLogit > maxLogit)
            throw new ArgumentException("minLogit must be <= maxLogit");

        using var connection = _database.GetConnection();
        return await connection.ExecuteAsync(
            """
            UPDATE AgentMemorys
            SET `ConfidenceLogit` = `ConfidenceLogit` + @value
            WHERE (@minLogit IS NULL OR `ConfidenceLogit` >= @minLogit)
              AND (@maxLogit IS NULL OR `ConfidenceLogit` <= @maxLogit)
            """,
            new
            {
                minLogit,
                maxLogit,
                value
            }
        );
    }

    /// <inheritdoc />
    public async Task<int> CleanupMemoryReferences(CancellationToken cancellation)
    {
        var affected = 0;

        using var connection = _database.GetConnection();
        using (var tsx = connection.BeginTransaction())
        {
            // Delete bad links (dangling MemoryId or EvidenceId)
            affected += await connection.ExecuteAsync(
                """
                DELETE FROM AgentMemoryEvidenceLinks
                WHERE MemoryId   NOT IN (SELECT ID FROM AgentMemorys)
                   OR EvidenceId NOT IN (SELECT ID FROM AgentMemoryEvidences)
                """,
                transaction: tsx
            );

            // Delete memories with no remaining evidence
            affected += await connection.ExecuteAsync(
                """
                DELETE FROM AgentMemorys AS m
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM AgentMemoryEvidenceLinks AS l
                    WHERE l.MemoryId = m.ID
                )
                """,
                transaction: tsx
            );

            // Delete evidence with no memories linked to it
            affected += await connection.ExecuteAsync(
                """
                DELETE FROM AgentMemoryEvidences AS e
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM AgentMemoryEvidenceLinks AS l
                    WHERE l.EvidenceId = e.ID
                )
                """,
                transaction: tsx
            );

            tsx.Commit();
        }

        return affected;
    }

    /// <inheritdoc />
    public async Task<bool> Delete(int id)
    {
        using var connection = _database.GetConnection();
        using (var tsx = connection.BeginTransaction())
        {
            await connection.ExecuteAsync(
                """
                DELETE FROM AgentMemoryEvidenceLinks
                WHERE MemoryId = @id
                """,
                new
                {
                    id = id
                },
                transaction: tsx
            );

            var count = await connection.ExecuteAsync(
                """
                DELETE FROM AgentMemorys
                WHERE ID = @id
                """,
                new
                {
                    id = id
                },
                transaction: tsx
            );

            tsx.Commit();

            return count > 0;
        }
    }

    /// <inheritdoc />
    public async Task UpdateAccessTime(int id)
    {
        using var connection = _database.GetConnection();

        await connection.ExecuteAsync(
            """
            UPDATE AgentMemorys
            SET `AccessUnix` = @UnixTime
            WHERE `ID` = @ID
            """,
            new
            {
                ID = id,
                UnixTime = DateTime.UtcNow.UnixTimestamp(),
            }
        );
    }
}