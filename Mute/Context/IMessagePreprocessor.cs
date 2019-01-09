namespace Mute.Context
{
    public interface IMessagePreprocessor
    {
        void Process(MuteCommandContext context);
    }
}
