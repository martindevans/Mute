using System;
using System.Threading.Tasks;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Discord.Services.Audio.Playback;
using Mute.Moe.Services.Speech.TTS;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules.Audio
{
    public class Voice
        : BaseModule
    {
        private readonly ITextToSpeech _tts;
        private readonly MultichannelAudioService _audio;
        private readonly SimpleQueueChannel<string> _channel;

        public Voice(ITextToSpeech tts, MultichannelAudioService audio)
        {
            _tts = tts;
            _audio = audio;
            _channel = (SimpleQueueChannel<string>)_audio.GetOrOpen("voice_module", () => new SimpleQueueChannel<string>());
        }

        [RequireOwner]
        [Command("tts")]
        [ThinkingReply(EmojiLookup.SpeakerMedVolume)]
        public async Task TextToSpeech([Remainder] string message)
        {
            if (!await _audio.MoveChannel(Context.User))
                await ReplyAsync("You are not in a voice channel");
            else
            {
                var audio = (await _tts.Synthesize(message)).Open();

                //Wait for audio to finish
                await _channel.Enqueue(message, audio);

                if (audio is IDisposable ad)
                    ad.Dispose();
            }
        }
    }
}
