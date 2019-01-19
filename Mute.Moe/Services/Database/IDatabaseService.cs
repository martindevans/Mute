using System.Data.Common;

namespace Mute.Moe.Services
{
    public interface IDatabaseService
    {
        DbCommand CreateCommand();
    }
}
