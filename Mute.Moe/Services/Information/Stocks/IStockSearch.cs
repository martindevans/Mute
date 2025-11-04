using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Stocks;

public interface IStockSearch
{
    IAsyncEnumerable<IStockSearchResult> Search(string search);

    Task<IStockInfo?> Lookup(string symbol);
}

public interface IStockSearchResult
{
    string Symbol { get; }

    string Name { get; }

    string Currency { get; }
}

public interface IStockInfo
{
    string Symbol { get; }
    string Name { get; }

    string? Description { get; }

    string Currency { get; }
}