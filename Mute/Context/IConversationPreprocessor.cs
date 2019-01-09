namespace Mute.Context
{
    public interface IConversationPreprocessor
    {
        void Process(MuteCommandContext context);
    }
}
