namespace Mute.Moe.Discord.Services.Responses.Eliza.Topics
{
    public class SingleMessageContext
        : ITopicDiscussion
    {
        private readonly string _message;

        public bool IsComplete => true;

        public SingleMessageContext(string message)
        {
            _message = message;
        }

        public (string, IKnowledge?) Reply(IKnowledge knowledge, IUtterance message)
        {
            return (_message, null);
        }
    }
}
