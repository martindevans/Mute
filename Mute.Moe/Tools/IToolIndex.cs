using System.Numerics.Tensors;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dapper;
using Mute.Moe.Services.Database;
using Dapper.Contrib.Extensions;
using Mute.Moe.Tools.Providers;
using Mute.Moe.Services.LLM.Embedding;
using Mute.Moe.Services.LLM.Rerank;
using Serilog;

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
    /// Fuzzy find tools for the given natural language query
    /// </summary>
    /// <param name="query"></param>
    /// <param name="topK">Only the K best results will be returned</param>
    /// <param name="topP">Only results with relevance over best * topP will be returned</param>
    /// <returns></returns>
    Task<IEnumerable<(float Relevance, ITool Tool)>> Find(string query, int topK = 5, float topP = 0.5f);
}

/// <inheritdoc />
public class DatabaseToolIndex
    : IToolIndex
{
    private static readonly ILogger _logger = Log.ForContext<DatabaseToolIndex>();

    /// <summary>
    /// Embeddings provider for tool descriptions
    /// </summary>
    private readonly IEmbeddings _embeddings;

    /// <summary>
    /// Reranker for tool queries
    /// </summary>
    private readonly IReranking _reranking;

    /// <inheritdoc />
    public IToolProvider[] Providers { get; }

    /// <inheritdoc />
    public IReadOnlyDictionary<string, ITool> Tools { get; }

    /// <summary>
    /// Map from tool name to embedding
    /// </summary>
    private readonly Task<IReadOnlyDictionary<string, ReadOnlyMemory<float>>> _toolEmbeddings;

    /// <summary>
    /// Create a new <see cref="DatabaseToolIndex"/>
    /// </summary>
    /// <param name="providers"></param>
    /// <param name="database"></param>
    /// <param name="embeddings"></param>
    /// <param name="reranking"></param>
    public DatabaseToolIndex(IEnumerable<IToolProvider> providers, IDatabaseService database, IEmbeddings embeddings, IReranking reranking)
    {
        _embeddings = embeddings;
        _reranking = reranking;
        Providers = [ ..providers ];

        // Build dictionary of tools by name
        Tools = (
            from provider in Providers
            from tool in provider.Tools
            select tool
        ).ToDictionary(a => a.Name, a => a);

        // Create table to cache embeddings
        using var db = database.GetConnection();
        db.Execute("CREATE TABLE IF NOT EXISTS `ToolDescriptionEmbeddings` (`Name` TEXT NOT NULL, `Description` TEXT NOT NULL, `Model` TEXT NOT NULL, `Embedding` BLOB)");

        // Start task to build embedding index
        _toolEmbeddings = Task.Run(async () => await BuildEmbeddingTable(Tools, database, embeddings));
    }

    private static async Task<IReadOnlyDictionary<string, ReadOnlyMemory<float>>> BuildEmbeddingTable(IReadOnlyDictionary<string, ITool> tools, IDatabaseService database, IEmbeddings embeddings)
    {
        using var db = database.GetConnection();

        // Delete all tools from DB which no longer exist or have a different description
        using (var tsx = db.BeginTransaction())
        {
            foreach (var item in await db.QueryAsync<ToolDescriptionEmbedding>("SELECT * From `ToolDescriptionEmbeddings`"))
                if (!tools.TryGetValue(item.Name, out var tool) || tool.Description != item.Description)
                    await db.ExecuteAsync("DELETE FROM `ToolDescriptionEmbeddings` WHERE (Name = @Name)", new { Name = item.Name }, tsx);

            tsx.Commit();
        }

        // Build dictionary of tool name => embedding
        var results = new Dictionary<string, ReadOnlyMemory<float>>();

        // Insert all tools into DB which aren't already there
        using (var tsx = db.BeginTransaction())
        {
            foreach (var (name, tool) in tools)
            {
                // Check description validity
                if (string.IsNullOrWhiteSpace(tool.Description))
                {
                    _logger.Warning("Tool '{name}' has empty/null description!", name);
                    await Console.Error.WriteLineAsync($"Tool '{name}' has empty/null description!");
                    continue;
                }

                // Try to get the embedding for this tool from the DB cache
                var embeddingBlob = await db.QuerySingleOrDefaultAsync<byte[]>(
                    "SELECT Embedding From `ToolDescriptionEmbeddings` WHERE (Name = @Name AND Model = @Model)",
                    new
                    {
                        Name = name,
                        Model = embeddings.Model
                    }
                );

                // If we didn't find a cached embedding, generate it now
                if (embeddingBlob == null)
                {
                    // Generate
                    var embeddingResult = await embeddings.Embed(tool.Description);
                    if (embeddingResult == null)
                    {
                        _logger.Warning("Tool '{name}' generated a null embedding!", name);
                        continue;
                    }

                    // Convert to BLOB
                    embeddingBlob = MemoryMarshal.Cast<float, byte>(embeddingResult.Result.Span).ToArray();

                    // Insert into DB cache
                    Log.Information("Inserting new tool: {0}", tool.Name);
                    await db.InsertAsync(
                        new ToolDescriptionEmbedding
                        {
                            Name = tool.Name,
                            Description = tool.Description,
                            Model = embeddings.Model,
                            Embedding = embeddingBlob,
                        },
                        tsx
                    );
                }

                // Convert blob to float
                var embeddingFloat = MemoryMarshal.Cast<byte, float>(embeddingBlob).ToArray();
                results.Add(tool.Name, embeddingFloat);
            }

            tsx.Commit();
        }

        return results;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<(float Relevance, ITool Tool)>> Find(string query, int topK = 5, float topP = 0.5f)
    {
        var embedding = await _embeddings.Embed(query);
        if (embedding == null)
            return [ ];


        // Get tools with dot product to query embedding
        var toolEmbeddings = await _toolEmbeddings;
        var results = (
            from item in Tools
            let name = item.Key
            let tool = item.Value
            where tool != null
            let toolEmbedding = toolEmbeddings.GetValueOrDefault(name, Array.Empty<float>())
            where !toolEmbedding.IsEmpty
            let dot = TensorPrimitives.Dot(embedding.Result.Span, toolEmbedding.Span)
            where !float.IsNaN(dot) && !float.IsInfinity(dot)
            orderby dot descending
            select (dot, tool)
        ).Take(topK).ToArray();

        // Early exit if there's no real re-ranker, just to skip the useless work
        if (_reranking is NullRerank)
            return TopP(results, topP);

        var rerankPrompt = $"""
                           Task: Score how appropriate each tool is for accomplishing the goal.
                           Goal: {query}
                           Instructions:
                            - Score high tools that help accomplish the goal
                            - Score low tools that are irrelevant
                            - Consider the functionality of the tool
                            - Prefer tools that directly enable the action
                           """;

        // Rerank the tools based on the query and their description
        var reranking = await _reranking.Rerank(rerankPrompt, results.Select(a => a.tool.Description).ToArray());

        // New list of results
        var rerankedResults = new List<(float, ITool)>();
        foreach (var rank in reranking)
            rerankedResults.Add((rank.Relevance, results[rank.Index].tool));

        return TopP(rerankedResults, topP); ;
    }

    private static IEnumerable<(float, ITool)> TopP(IEnumerable<(float Relevance, ITool Tool)> values, float topP)
    {
        float? first = default;
        foreach (var (relevance, item) in values)
        {
            if (!first.HasValue)
            {
                first = relevance;
            }
            else
            {
                if (relevance < first * topP)
                    yield break;
            }

            yield return (relevance, item);
        }
    }

    private class ToolDescriptionEmbedding
    {
        public required string Name { get; init; }

        public required string Description { get; init; }
        public required string Model { get; init; }

        public byte[] Embedding { get; init; } = [ ];
    }
}