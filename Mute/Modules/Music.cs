using System;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Addons.Interactive;
using Discord.Audio;
using Discord.Commands;
using Mute.Extensions;
using Mute.Services;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Modules
{
    public class Music
        : InteractiveBase
    {
        private readonly FileSystemService _fs;

        public Music(FileSystemService fs)
        {
            _fs = fs;
        }

        [Command("play")]
        [RequireOwner]
        public async Task Play(string name)
        {
            try
            {
                //Find the file and early exit if we cannot
                var f = new FileInfo(Path.Combine(@"C:\Users\Martin\Documents\dotnet\Mute\Test Music\", name));
                if (f == null || !f.Exists)
                {
                    await this.TypingReplyAsync($"Cannot find file `{name}`");
                    return;
                }

                //Check if the user in a voice channel, if so join it
                if (Context.User is IVoiceState v)
                {
                    using (var client = await v.VoiceChannel.ConnectAsync())
                    {
                        await client.SetSpeakingAsync(true);

                        var discord = client.CreatePCMStream(AudioApplication.Mixed, v.VoiceChannel.Bitrate, 200);
                        var format = new WaveFormat(48000, 16, 2);

                        using (var audioFile = new AudioFileReader(f.FullName))
                        using (var resampler = new MediaFoundationResampler(audioFile, format))
                        {
                            // Set resampler quality to max
                            resampler.ResamplerQuality = 60;

                            // Copy the data in ~200ms blocks
                            var blockSize = format.AverageBytesPerSecond / 50;
                            var buffer = new byte[blockSize];
                            int byteCount;
                            while ((byteCount = resampler.Read(buffer, 0, blockSize)) > 0)
                            {
                                // Zero out incomplete Frame
                                if (byteCount < blockSize)
                                    for (var i = byteCount; i < blockSize; i++)
                                        buffer[i] = 0;

                                await discord.WriteAsync(buffer, 0, blockSize);
                            }

                            await discord.FlushAsync();
                        }

                        await Task.Delay(100);

                        await client.SetSpeakingAsync(false);
                    }
                }
                else
                {
                    await ReplyAsync("You are not in a voice channel");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
