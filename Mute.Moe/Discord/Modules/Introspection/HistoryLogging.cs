using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;

namespace Mute.Moe.Discord.Modules.Introspection
{
    [RequireOwner]
    public class HistoryLogging
        : BaseModule
    {
        [Command("log")]
        [ThinkingReply]
        public async Task Log(int count = 10, bool ignoreBots = true)
        {
            var status = await ReplyAsync("Downloading...");

            var timer = new Stopwatch();
            timer.Start();

            await using (var mem = new MemoryStream(count * 20))
            {
                await using (var writer = new StreamWriter(mem, Encoding.UTF8, 10, true))
                {
                    //Write out all the messages to the writer
                    await foreach (var msg in ScrapeChannel(Context.Channel).Take(count))
                    {
                        //Skip message from bot user if necessary
                        if (ignoreBots && msg.Author.IsBot)
                            return;

                        //Write message
                        await writer.WriteLineAsync($"[{msg.Author.Id}] {msg.Content}");
                    }
                }

                //Rewind memory buffer
                mem.Position = 0;

                //Send it as a text file
                await Context.Channel.SendFileAsync(mem, "log.txt");
            }

            await status.ModifyAsync(m => m.Content = "Done!");
        }

        private static async IAsyncEnumerable<IMessage> ScrapeChannel(IMessageChannel channel, IMessage? start = null)
        {
            //If start message is not set then get the latest message in the channel now
            start ??= (await channel.GetMessagesAsync(1).FlattenAsync()).SingleOrDefault();

            // Keep loading pages until the start message is null
            while (start != null)
            {
                // Add a slight delay between fetching pages so we don't hammer discord too hard
                await Task.Delay(150);

                // Get the next page of messages
                var page = (await channel.GetMessagesAsync(start, Direction.Before, 99).FlattenAsync()).OrderByDescending(a => a.CreatedAt).ToArray();

                // Set the start of the next page to the end of this page
                start = page.LastOrDefault();

                // yield every message in page
                foreach (var message in page)
                    yield return message;
            }
        }
    }
}
