using System.Threading.Tasks;


namespace Mute.Moe.Discord.Context.Preprocessing
{
    public interface IMessagePreprocessor
    {
         Task Process( MuteCommandContext context);
    }
}
