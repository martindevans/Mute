using System.Collections.Generic;

namespace Mute.Moe.Services.Information.Stocks
{
    public interface IStockSearch
    {
        IAsyncEnumerable<IStockSearchResult> Search(string search);
    }

    public interface IStockSearchResult
    {
        string Symbol { get; }

        string Name { get; }

        string Currency { get; }
    }
}
