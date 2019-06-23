using System.Threading.Tasks;
using Discord.Commands;
using JetBrains.Annotations;

namespace Mute.Moe.Discord.Context.Postprocessing
{
    public interface IUnsuccessfulCommandPostprocessor
    {
        uint Order { get; }

        Task<bool> Process([NotNull] MuteCommandContext context, [NotNull] IResult result);
    }
}
