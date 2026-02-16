using System.Threading.Tasks;
using Humanizer;
using Mute.Moe.Services.LLM.Memory;
using Mute.Moe.Services.LLM.Rerank;

namespace Mute.Moe.Tools.Providers
{
    /// <summary>
    /// Provides tools for managing agent memories
    /// </summary>
    public class MemoryManagementProvider
        : IToolProvider
    {
        private readonly IAgentMemoryStorage _memory;
        private readonly IReranking _reranking;

        /// <inheritdoc />
        public IReadOnlyList<ITool> Tools { get; }

        /// <summary>
        /// Create a new <see cref="MemoryManagementProvider"/>
        /// </summary>
        /// <param name="memory"></param>
        /// <param name="reranking"></param>
        public MemoryManagementProvider(IAgentMemoryStorage memory, IReranking reranking)
        {
            _memory = memory;
            _reranking = reranking;

            Tools =
            [
                new AutoTool("memory_search", isDefault:true, BasicMemorySearch),
            ];
        }

        /// <summary>
        /// Searches through agent memories for information that may help answers a specific query.<br />
        /// - Capability: Agent memory search and retrieval.<br />
        /// - Inputs: Query for information potentially stored in memory.<br />
        /// - Outputs: A list of memories, ranked by relevance.
        /// </summary>
        /// <param name="query">Query for information potentially stored in memory.</param>
        /// <returns></returns>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
        private async Task<object> BasicMemorySearch(ITool.CallContext callCtx, string query)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
        {
            var context = callCtx.Channel.GetAgentMemoryContextId();

            // Simple embedding similarity search
            var memories = (await _memory.FindSimilar(
                context,
                query,
                32
            )).ToArray();

            // This shouldn't ever happen unless there are no memories stored at all!
            if (memories.Length == 0)
                return new { error = "No relevant memories were found" };

            var rerankPrompt = $"""
                                Task: Score how relevant each fact is for answering the given query.
                                Query: '{query}'
                                Instructions:
                                 - Score high facts that are relevant to the query
                                 - Score low facts that are irrelevant
                                """;

            // Rerank the memories based on the query and their description
            var reranking = await _reranking.Rerank(rerankPrompt, memories.Select(a => a.Memory.Text).ToArray());

            // Establish cutoff for relevance (half of top item)
            var cutoff = reranking[0].Relevance * 0.5f;

            // Create outputs, in relevance order (applying cutoff)
            var rerankedResults = new List<object>();
            foreach (var rank in reranking)
            {
                if (rank.Relevance >= cutoff)
                {
                    // Create output object
                    var memory = memories[rank.Index].Memory;
                    rerankedResults.Add(new
                    {
                        Relevance = rank.Relevance.ToString("P2"),
                        Memory = memory.Text,
                        Confidence = memory.ConfidenceLogit.LogitToProbability().ToString("P2"),
                        LastAccess = memory.Access.Humanize(),
                    });

                    // Update access time to now
                    await _memory.UpdateAccessTime(memory.ID);
                }
            }

            return rerankedResults;
        }
    }
}
