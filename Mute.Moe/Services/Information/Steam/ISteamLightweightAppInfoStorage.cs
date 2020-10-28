using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Steam
{
    public interface ISteamLightweightAppInfoStorage
    {
        Task<ILightweightAppInfoModel?> Get(uint appId);
    }

    public interface ILightweightAppInfoModel
    {
        string Name { get; }

        string ShortDescription { get; }

        uint AppId { get; }
    }
}
