using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;

namespace Mute.Modules
{
    public class Administration
        : ModuleBase
    {
        private readonly DatabaseService _database;

        public Administration(DatabaseService database)
        {
            _database = database;
        }

        [Command("say"), Summary("I will say whatever you want, but I won't be happy about it >:(")]
        [RequireOwner]
        public async Task Say([Remainder] string s)
        {
            await this.TypingReplyAsync(s);
        }

        [Command("sql"), Summary("I will execute an arbitrary SQL statement. Please be very careful x_x")]
        public async Task Sql([Remainder] string sql)
        {
            using (var result = await _database.ExecReader(sql))
                await this.TypingReplyAsync($"SQL affected {result.RecordsAffected} rows");
        }

        [Command("music"), Summary("Test command for voice integration")]
        [RequireOwner]
        public async Task JoinVoice()
        {
            if (Context.User is IVoiceState v)
            {
                using (var client = await v.VoiceChannel.ConnectAsync())
                {
                    await client.SetSpeakingAsync(true);

                    Console.WriteLine("Starting writing");

                    using (var bytes = File.OpenRead("piano.pcm"))
                    {
                        var discord = client.CreatePCMStream(AudioApplication.Mixed, 90000, 200);
                        await bytes.CopyToAsync(discord);
                        await discord.FlushAsync();
                    }

                    await Task.Delay(1000);

                    await client.SetSpeakingAsync(false);
                }
            }
        }
    }
}
