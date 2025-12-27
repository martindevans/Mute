using LlmTornado.Chat;
using static System.Net.Mime.MediaTypeNames;

namespace Mute.Moe.Extensions;

/// <summary>
/// Extensions for <see cref="ChatRichResponse"/>
/// </summary>
public static class ChatRichResponseExtensions
{
    /// <summary>
    /// Extensions for <see cref="ChatRichResponse"/>
    /// </summary>
    /// <param name="self"></param>
    extension(ChatRichResponse self)
    {
        /// <summary>
        /// Get all reasoning text combined together
        /// </summary>
        /// <returns></returns>
        public string GetReasoningText(string separator = " ")
        {
            return string.Join(
                separator,
                self.GetBlocks(ChatRichResponseBlockTypes.Reasoning).Select(x => x.Reasoning?.Content ?? "")
            );
        }

        /// <summary>
        /// Get all reasoning text combined together
        /// </summary>
        /// <returns></returns>
        public string GetReasoningLength(string separator = " ")
        {
            return string.Join(
                separator,
                self.GetBlocks(ChatRichResponseBlockTypes.Reasoning).Select(x => x.Reasoning?.Content ?? "")
            );
        }

        /// <summary>
        /// Get all blocks of a given type
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public IEnumerable<ChatRichResponseBlock> GetBlocks(ChatRichResponseBlockTypes type)
        {
            foreach (var block in self.Blocks)
            {
                if (block.Type != type)
                    continue;

                yield return block;
            }
        }
    }
}