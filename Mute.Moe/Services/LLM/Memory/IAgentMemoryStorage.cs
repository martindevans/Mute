using Dapper;
using Dapper.Contrib.Extensions;
using Mute.Moe.Services.Database;
using Mute.Moe.Services.LLM.Embedding;
using Serilog;
using System.Data;
using System.Runtime.InteropServices;
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
    public Task<int?> CreateMemory(ulong context, string text, float confidenceLogit, IDbTransaction? tsx = null);

    /// <summary>
    /// Create an evidence object, return the ID
    /// </summary>
    /// <param name="context"></param>
    /// <param name="text"></param>
    /// <param name="tsx"></param>
    /// <returns></returns>
    public Task<int> CreateEvidence(ulong context, string text, IDbTransaction? tsx = null);

    /// <summary>
    /// Create a link between a memory and some evidence
    /// </summary>
    /// <param name="evidence"></param>
    /// <param name="memory"></param>
    /// <param name="tsx"></param>
    /// <returns></returns>
    public Task CreateEvidenceLink(int evidence, int memory, IDbTransaction? tsx = null);

    /// <summary>
    /// Retrieve a specific memory by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<AgentMemory> Get(int id);

    /// <summary>
    /// Find memories with similar embedding to the query
    /// </summary>
    /// <param name="context"></param>
    /// <param name="query"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    public Task<IEnumerable<AgentMemory>> FindSimilar(ulong context, string query, int limit);

    /// <summary>
    /// Add a value to all confidence logits within a range
    /// </summary>
    /// <param name="minLogit">Logits below this threshold will NOT be updated</param>
    /// <param name="maxLogit">Logits above this threshold will NOT be updated</param>
    /// <param name="value">Value to add directly to logit</param>
    /// <param name="tsx">Transaction (optional)</param>
    /// <returns></returns>
    public Task<int> AddToConfidenceLogits(float? minLogit, float? maxLogit, float value, IDbTransaction? tsx = null);

    /// <summary>
    /// Delete all memories that do not have a linked evidence items
    /// </summary>
    /// <returns></returns>
    public Task<int> DeleteMemoryWithoutEvidence();
}

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

        _database.Exec("""
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

        _database.Exec("""
                       CREATE TABLE IF NOT EXISTS `AgentMemoryEvidences`
                       (
                           `ID`              INTEGER PRIMARY KEY ASC,
                           `Context`         INTEGER,
                           `Text`            TEXT NOT NULL,
                           `CreationUnix`    INTEGER,
                           `AccessUnix`      INTEGER
                       );
                       """);
        
        _database.Exec("""
                       CREATE TABLE IF NOT EXISTS `AgentMemoryEvidenceLinks`
                       (
                           `EvidenceId`      INTEGER,
                           `MemoryId`        INTEGER,
                           FOREIGN KEY(EvidenceId) REFERENCES AgentMemoryEvidences(ID),
                           FOREIGN KEY(MemoryId)   REFERENCES AgentMemorys(ID)
                       );
                       """);

        // Initialise the column as a vector store
        _database.Exec($"SELECT vector_init('AgentMemorys', 'Embedding', 'type=FLOAT32,dimension={_embeddings.Dimensions},distance=cosine');");
        _database.Exec("SELECT vector_quantize('AgentMemorys', 'Embedding')");

        // Add indices
        _database.Exec("CREATE INDEX IF NOT EXISTS `AgentMemorysByContext` ON `AgentMemorys` (`Context` ASC);");
        _database.Exec("CREATE INDEX IF NOT EXISTS `AgentMemorysByConfidence` ON `AgentMemorys` (`ConfidenceLogit` ASC);");
        _database.Exec("CREATE INDEX IF NOT EXISTS `AgentMemoryEvidenceLinksByMemoryId` ON AgentMemoryEvidenceLinks(MemoryId);");
        _database.Exec("CREATE INDEX IF NOT EXISTS `AgentMemoryEvidenceLinksOnEvidenceId` ON AgentMemoryEvidenceLinks(EvidenceId);");

        // Check for incorrect embeddings
        var count = _database.Connection.ExecuteScalar<int>(
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
            // Get a batch of items
            var items = (await _database.Connection.QueryAsync<AgentMemory>("SELECT * FROM AgentMemorys WHERE (EmbeddingModel != @EmbeddingModel) LIMIT 8")).ToList();
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

                await _database.Connection.ExecuteAsync(
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

            // Long delay between batches
            await Task.Delay(10);
        }
    }

    /// <inheritdoc />
    public async Task<int?> CreateMemory(ulong context, string text, float confidenceLogit, IDbTransaction? tsx = null)
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

        return await _database.Connection.InsertAsync(memory, tsx);
    }

    /// <inheritdoc />
    public async Task<int> CreateEvidence(ulong context, string text, IDbTransaction? tsx = null)
    {
        var now = DateTime.UtcNow.UnixTimestamp();
        var evidence = new AgentMemoryEvidence
        {
            Context = context,
            Text = text,
            CreationUnix = now,
            AccessUnix = now,
        };

        return await _database.Connection.InsertAsync(evidence, tsx);
    }

    /// <inheritdoc />
    public async Task CreateEvidenceLink(int evidence, int memory, IDbTransaction? tsx = null)
    {
        var link = new AgentMemoryEvidenceLink
        {
            EvidenceId = evidence,
            MemoryId = memory
        };

        await _database.Connection.InsertAsync(link, tsx);
    }

    /// <inheritdoc />
    public Task<AgentMemory> Get(int id)
    {
        return _database.Connection.GetAsync<AgentMemory>(id);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AgentMemory>> FindSimilar(ulong context, string query, int limit)
    {
        var queryEmbedding = await _embeddings.Embed(query);
        if (queryEmbedding == null)
        {
            Log.Warning("Failed to embed memory query string");
            return [ ];
        }

        const string SQL = """
                           SELECT t.*
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

        var result = _database.Connection.Query<AgentMemory>(SQL, new
        {
            Context = context,
            QueryEmbedding = MemoryMarshal.Cast<float, byte>(queryEmbedding.Result.Span).ToArray(),
            TopK = limit,
            EmbeddingModel = queryEmbedding.Model
        });

        return result;
    }

    /// <inheritdoc />
    public async Task<int> AddToConfidenceLogits(float? minLogit, float? maxLogit, float value, IDbTransaction? tsx = null)
    {
        if (minLogit > maxLogit)
            throw new ArgumentException("minLogit must be <= maxLogit");

        return await _database.Connection.ExecuteAsync(
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
            },
            tsx
        );
    }

    /// <inheritdoc />
    public async Task<int> DeleteMemoryWithoutEvidence()
    {
        var affected = 0;

        using var tx = _database.Connection.BeginTransaction();

        // Delete bad links (dangling MemoryId or EvidenceId)
        affected += await _database.Connection.ExecuteAsync(
            """
            DELETE FROM AgentMemoryEvidenceLinks
            WHERE MemoryId   NOT IN (SELECT ID FROM AgentMemorys)
               OR EvidenceId NOT IN (SELECT ID FROM AgentMemoryEvidences)
            """,
            transaction: tx
        );

        // Delete memories with no remaining evidence
        affected += await _database.Connection.ExecuteAsync(
            """
            DELETE FROM AgentMemorys AS m
            WHERE NOT EXISTS (
                SELECT 1
                FROM AgentMemoryEvidenceLinks AS l
                WHERE l.MemoryId = m.ID
            )
            """,
            transaction: tx
        );

        tx.Commit();
        return affected;
    }
}