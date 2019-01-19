namespace Mute.Moe.Discord.Context
{
    public interface IMessagePreprocessor
    {
        void Process(MuteCommandContext context);
    }
}
