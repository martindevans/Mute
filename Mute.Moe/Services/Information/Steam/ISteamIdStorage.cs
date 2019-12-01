using System.Threading.Tasks;

namespace Mute.Moe.Services.Information.Steam
{
    public interface ISteamIdStorage
    {
        Task Set(ulong discordId, ulong steamId);

        Task<ulong?> Get(ulong discordId);
    }
}
