namespace Mute.Moe.Discord.Context
{
    public interface IConversationPreprocessor
    {
        void Process(MuteCommandContext context);
    }
}
