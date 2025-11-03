using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dapper;
using Mute.Moe.Services.Database;
using Dapper.Contrib.Extensions;
using Mute.Moe.Services.LLM;

namespace Mute.Moe.Tools;

/// <summary>
/// Stores available tools
/// </summary>
public interface IToolIndex
{
    /// <summary>
    /// All available tool providers
    /// </summary>
    IToolProvider[] Providers { get; }

    /// <summary>
    /// All available tools
    /// </summary>
    IReadOnlyDictionary<string, ITool> Tools { get; }

    /// <summary>
    /// Do one time update of the index
    /// </summary>
    /// <returns></returns>
    Task Update();

    /// <summary>
    /// Fuzzy find tools for the given natural language query
    /// </summary>
    /// <param name="query"></param>
    /// <param name="limit"></param>
    /// <returns></returns>
    Task<IEnumerable<(float Similarity, ITool Tool)>> Find(string query, int limit);
}

/// <inheritdoc />
public class DatabaseToolIndex
    : IToolIndex
{
    /// <summary>
    /// The database backing this index
    /// </summary>
    private readonly IDatabaseService _database;

    /// <summary>
    /// Embeddings provider for tool descriptions
    /// </summary>
    private readonly IEmbeddings _embeddings;

    /// <summary>
    /// Indicates if the <see cref="Update"/> method has been run
    /// </summary>
    private bool _updated;

    /// <inheritdoc />
    public IToolProvider[] Providers { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ITool> Tools { get; }

    /// <summary>
    /// Create a new <see cref="DatabaseToolIndex"/>
    /// </summary>
    /// <param name="providers"></param>
    /// <param name="database"></param>
    /// <param name="embeddings"></param>
    public DatabaseToolIndex(IEnumerable<IToolProvider> providers, IDatabaseService database, IEmbeddings embeddings)
    {
        _database = database;
        _embeddings = embeddings;
        Providers = providers.ToArray();

        Tools = (
            from provider in Providers
            from tool in provider.Tools
            select tool
        ).ToDictionary(a => a.Name, a => a);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<(float Similarity, ITool Tool)>> Find(string query, int limit)
    {
        await Update();

        var embedding = await _embeddings.Embed(query);
        if (embedding == null)
            return [ ];

        const string SQL = """
            SELECT t.Name, v.distance
            FROM ToolDescriptionEmbeddings AS t
            JOIN vector_quantize_scan(
                'ToolDescriptionEmbeddings',
                'Embedding',
                @QueryEmbedding,
                @TopK
            ) AS v
            ON t.rowid = v.rowid
            ORDER BY v.distance ASC;
        """;

        var result = _database.Connection.Query<(string Name, float distance)>(SQL, new
        {
            QueryEmbedding = MemoryMarshal.Cast<float, byte>(embedding.Result.Span).ToArray(),
            TopK = limit
        });

        return from r in result
               let similarity = (2 - r.distance) / 2
               where !float.IsNaN(similarity)
               where !float.IsInfinity(similarity)
               let tool = Tools.GetValueOrDefault(r.Name)
               where tool != null
               select (similarity, tool);
    }

    /// <inheritdoc />
    public async Task Update()
    {
        if (_updated)
            return;
        _updated = true;

        // Create table
        _database.Exec("CREATE TABLE IF NOT EXISTS `ToolDescriptionEmbeddings` (`Name` TEXT NOT NULL, `Description` TEXT NOT NULL, `Model` TEXT NOT NULL, `Embedding` BLOB)");

        // Delete all tools from DB which no longer exist in the toolset or have a different description
        var db = _database.Connection;
        using (var tsx = db.BeginTransaction())
        {
            foreach (var item in await db.QueryAsync<ToolDescriptionEmbedding>("SELECT * From `ToolDescriptionEmbeddings`"))
                if (!Tools.TryGetValue(item.Name, out var tool) || tool.Description != item.Description)
                    await db.ExecuteAsync("DELETE FROM `ToolDescriptionEmbeddings` WHERE (Name = @Name)", new { Name = item.Name }, tsx);

            tsx.Commit();
        }

        // Delete all tools from the DB which have a different embedding model
        using (var tsx = db.BeginTransaction())
        {
            await db.ExecuteAsync("DELETE FROM `ToolDescriptionEmbeddings` WHERE (Model != @Model)", new { Model = _embeddings.Model }, tsx);
            tsx.Commit();
        }

        // Insert all tools into DB which aren't already there
        using (var tsx = db.BeginTransaction())
        {
            foreach (var (name, tool) in Tools)
            {
                var count = await db.ExecuteScalarAsync<int>(
                    "SELECT Count(*) FROM `ToolDescriptionEmbeddings` WHERE (Name = @Name)",
                    new
                    {
                        Name = name,
                        Model = _embeddings.Model
                    }
                );

                if (0 == count)
                {
                    var embedding = await _embeddings.Embed(tool.Description);
                    if (embedding == null)
                        continue;

                    await db.InsertAsync(
                        new ToolDescriptionEmbedding
                        {
                            Name = tool.Name,
                            Description = tool.Description,
                            Model = embedding.Model,
                            Embedding = MemoryMarshal.Cast<float, byte>(embedding.Result.Span).ToArray(),
                        },
                        tsx
                    );
                }
            }

            tsx.Commit();
        }

        // Initialise the column as a vector store
        await db.ExecuteAsync($"SELECT vector_init('ToolDescriptionEmbeddings', 'Embedding', 'type=FLOAT32,dimension={_embeddings.Dimensions},distance=cosine');");
        await db.ExecuteAsync( "SELECT vector_quantize('ToolDescriptionEmbeddings', 'Embedding')");
    }

    private class ToolDescriptionEmbedding
    {
        public required string Name { get; set; }

        public required string Description { get; set; }
        public required string Model { get; set; }

        public byte[] Embedding { get; set; } = [ ];
    }
}