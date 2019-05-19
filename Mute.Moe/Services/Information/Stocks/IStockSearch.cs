using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Services.Information.Stocks
{
    public interface IStockSearch
    {
        [ItemCanBeNull] Task<IAsyncEnumerable<IStockSearchResult>> Search(string search);
    }

    public interface IStockSearchResult
    {
        string Symbol { get; }

        string Name { get; }

        string Currency { get; }
    }
}
