using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Mute.Moe.Discord.Context.Preprocessing
{
    public interface IMessagePreprocessor
    {
        [NotNull] Task Process([NotNull] MuteCommandContext context);
    }
}
