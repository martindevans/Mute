using System.Threading.Tasks;


namespace Mute.Moe.Discord.Context.Preprocessing
{
    /// <summary>
    /// Preprocess every message received by the bot
    /// </summary>
    public interface IMessagePreprocessor
    {
         Task Process(MuteCommandContext context);
    }
}
