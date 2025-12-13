//using System.Threading.Tasks;
//using LlmTornado.Rerank;

//namespace Mute.Moe.Services.LLM.Rerank;

///// <inheritdoc />
//public class TornadoReranking
//    : IReranking
//{
//    private readonly RerankModelEndpoint _reranking;

//    /// <summary>
//    /// 
//    /// </summary>
//    /// <param name="reranking"></param>
//    public TornadoReranking(RerankModelEndpoint reranking)
//    {
//        _reranking = reranking;
//    }

//    /// <inheritdoc />
//    public async Task<List<RerankResult>> Rerank(string query, IReadOnlyList<string> documents)
//    {
//        var result = await _reranking.Api.Rerank.CreateRerank(
//            new RerankRequest(
//                _reranking.Model,
//                query,
//                documents.ToList()
//            )
//        );

//        var output = new List<RerankResult>();
//        foreach (var item in result!.Data)
//            output.Add(new RerankResult(item.Index, item.RelevanceScore));

//        output.Sort();
//        output.Reverse();

//        return output;
//    }
//}