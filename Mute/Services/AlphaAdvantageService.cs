namespace Mute.Services
{
    public interface IStockService
    {

    }

    public class AlphaAdvantageService
        : IStockService
    {
        public AlphaAdvantageService(AlphaAdvantageConfig config)
        {
            //https://www.alphavantage.co/documentation/#fx
        }
    }
}
