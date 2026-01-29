using Dapper;
using Dapper.Contrib.Extensions;
using Mute.Moe.Services.Database;
using System.Data;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Mute.Moe.Services.LLM.Embedding;
using Serilog;

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
    /// Retrieve a specific memory by ID
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Task<AgentMemory> Get(int id);

    /// <summary>
    /// Get memories, filtered by SPO
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
    public Task<IEnumerable<AgentMemory>> FindSimilar(ulong context, string query, int limit);

    /// <summary>
    /// Update all confidence logits which are below a certain threshold
    /// </summary>
    /// <param name="confidenceLogitThreshold">Logits below this threshold will be updated</param>
    /// <param name="factor">Logits will be multipled by this value</param>
    /// <param name="tsx">Transaction (optional)</param>
    /// <returns></returns>
    public Task<int> UpdateConfidenceDecay(float confidenceLogitThreshold, float factor, IDbTransaction? tsx = null);
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
                       CREATE TABLE IF NOT EXISTS `AgentMemoryLinks`
                       (
                           `ID`        INTEGER PRIMARY KEY ASC,
                           `MemorySrc` INTEGER,
                           `MemoryDst` INTEGER,
                           `Type`      INTEGER,
                           FOREIGN KEY(MemorySrc) REFERENCES AgentMemorys(ID),
                           FOREIGN KEY(MemoryDst) REFERENCES AgentMemorys(ID)
                       )
                       """);

        // Initialise the column as a vector store
        _database.Exec($"SELECT vector_init('AgentMemorys', 'Embedding', 'type=FLOAT32,dimension={_embeddings.Dimensions},distance=cosine');");
        _database.Exec("SELECT vector_quantize('AgentMemorys', 'Embedding')");
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
    public Task<AgentMemory> Get(int id)
    {
        return _database.Connection.GetAsync<AgentMemory>(id);
    }

    /// <inheritdoc />
    public Task<IEnumerable<AgentMemory>> Get(ulong context)
    {
        return _database.Connection.QueryAsync<AgentMemory>(
            """
            SELECT *
            FROM AgentMemorys
            WHERE `Context` = @context
            """,
            new
            {
                context = context,
            }
        );
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
                           ORDER BY v.distance ASC
                           LIMIT @TopK;
                           """;

        var result = _database.Connection.Query<AgentMemory>(SQL, new
        {
            Context = context,
            QueryEmbedding = MemoryMarshal.Cast<float, byte>(queryEmbedding.Result.Span).ToArray(),
            TopK = limit
        });

        return result;
    }

    /// <inheritdoc />
    public async Task<int> UpdateConfidenceDecay(float confidenceLogitThreshold, float factor, IDbTransaction? tsx = null)
    {
        return await _database.Connection.ExecuteAsync(
            """
            UPDATE AgentMemorys
            SET `ConfidenceLogit` = `ConfidenceLogit` * @factor
            WHERE `ConfidenceLogit` < @confidenceLogitThreshold
            """,
            new
            {
                confidenceLogitThreshold,
                factor
            },
            tsx
        );
    }
}