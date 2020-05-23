using System.Collections.Generic;

namespace Mute.Moe.Discord.Services.Responses.Eliza.Topics
{
    /// <summary>
    /// A provider of new topic keys for the chat script
    /// </summary>
    public interface ITopicKeyProvider
    {
        /// <summary>
        /// A set of topic keys provided by this
        /// </summary>
         IEnumerable<ITopicKey> Keys { get; }
    }

    /// <summary>
    /// A potential topic the bot can talk about
    /// </summary>
    public interface ITopicKey
    {
        /// <summary>
        /// Get the keyword which will trigger an attempt to use this key
        /// </summary>
         string Keyword { get; }

        /// <summary>
        /// Get the rank of this key, higher ranks will be matched first
        /// </summary>
        int Rank { get; }

        /// <summary>
        /// Try to start a conversation on this topic in response to the given message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        ITopicDiscussion TryBegin(IUtterance message);
    }

    /// <summary>
    /// A specific discussion regarding a topic
    /// </summary>
    public interface ITopicDiscussion
    {
        bool IsComplete { get; }

        (string, IKnowledge?) Reply(IKnowledge knowledge, IUtterance message);
    }

    /// <summary>
    /// A set of knowledge the bot knows
    /// </summary> 
    public interface IKnowledge
    {
        string Who { get; }
        string What { get; }
        string When { get; }
        string Where { get; }
    }
}
