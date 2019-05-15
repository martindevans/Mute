using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using JetBrains.Annotations;
using Mute.Moe.AsyncEnumerable;
using Mute.Moe.AsyncEnumerable.Extensions;
using Mute.Moe.Discord.Attributes;

namespace Mute.Moe.Discord.Modules
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
            var self = Context.Client.CurrentUser.Id;

            var timer = new Stopwatch();
            timer.Start();

            using (var mem = new MemoryStream(count * 20))
            {
                using (var writer = new StreamWriter(mem, Encoding.UTF8, 10, true))
                {
                    //Write out all the messages to the writer
                    await (await ScrapeChannel(Context.Channel).Take(count)).ForEachAsync(async msg => {

                        //Skip message from bot user if necessary
                        if (ignoreBots && msg.Author.IsBot)
                            return;

                        //Write message
                        await writer.WriteLineAsync($"[{msg.Author.Id}] {msg.Content}");

                    });
                }

                //Rewind memory buffer
                mem.Position = 0;

                //Send it as a text file
                await Context.Channel.SendFileAsync(mem, "log.txt");
            }

            await status.ModifyAsync(m => m.Content = "Done!");
        }

        [ItemNotNull]
        private static async Task<IAsyncEnumerable<IMessage>> ScrapeChannel([NotNull] IMessageChannel channel, IMessage start = null)
        {
            //If start message is not set then get the latest message in the channel now
            if (start == null)
                start = (await channel.GetMessagesAsync(1).FlattenAsync()).SingleOrDefault();

            //If message is still null that means we failed to get a start message (no messages in channel?)
            if (start == null)
                return new EmptyAsyncEnumerable<IMessage>();

            return new PagedAsyncEnumerable<IReadOnlyList<IMessage>, IMessage>(
                async page => {

                    //Add a slight delay between fetching pages so we don't hammer discord too hard
                    await Task.Delay(150);

                    var startMessage = start;
                    if (page != null)
                        startMessage = page.LastOrDefault();

                    if (startMessage == null)
                        return Array.Empty<IMessage>();

                    return (await channel.GetMessagesAsync(startMessage, Direction.Before, 99).FlattenAsync()).OrderByDescending(a => a.CreatedAt).ToArray();

                },
                page => page.GetEnumerator()
            );
        }
    }
}
