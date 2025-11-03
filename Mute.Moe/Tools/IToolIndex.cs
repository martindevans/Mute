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
    /// <returns></returns>
    Task<IEnumerable<(float Similarity, ITool Tool)>> Find(string query);
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
    public async Task<IEnumerable<(float, ITool)>> Find(string query)
    {
        await Update();

        var result = await _embeddings.Embed(query);
        if (result == null)
            return [ ];

        var tools = await _database.Connection.QueryAsync<ToolDescriptionEmbedding>("SELECT * FROM ToolDescriptionEmbeddings WHERE (Model = @Model)", new
        {
            Model = _embeddings.Model
        });

        return from toolEmb in tools
               let similarity = TensorPrimitives.CosineSimilarity(MemoryMarshal.Cast<byte, float>(toolEmb.Embedding.AsSpan()), result.Result.Span)
               where !float.IsNaN(similarity)
               where !float.IsInfinity(similarity)
               orderby similarity descending 
               let tool = Tools.GetValueOrDefault(toolEmb.Name)
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

        // Insert all tools into DB which aren't already there
        using (var tsx = db.BeginTransaction())
        {
            foreach (var (name, tool) in Tools)
            {
                var count = await db.ExecuteScalarAsync<int>(
                    "SELECT Count(*) FROM `ToolDescriptionEmbeddings` WHERE (Name = @Name) AND (Model = @Model)",
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
    }

    private class ToolDescriptionEmbedding
    {
        public required string Name { get; set; }

        public required string Description { get; set; }
        public required string Model { get; set; }

        public byte[] Embedding { get; set; } = [ ];
    }
}