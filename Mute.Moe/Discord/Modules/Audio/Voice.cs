using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Audio;
using Mute.Moe.Services.Speech.TTS;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules.Audio
{
    public class Voice
        : BaseModule
    {
        private readonly ITextToSpeech _tts;
        private readonly IGuildVoiceCollection _guildAudio;

        public Voice(ITextToSpeech tts, IGuildVoiceCollection guildAudio)
        {
            _tts = tts;
            _guildAudio = guildAudio;
        }

        [RequireOwner]
        [Command("tts")]
        [ThinkingReply(EmojiLookup.SpeakerMedVolume)]
        public async Task TextToSpeech([Remainder] string message)
        {
            var user = Context.User;

            if (!(user is IVoiceState vs))
            {
                await ReplyAsync("You are not in a voice channel");
            }
            else
            {
                var player = await _guildAudio.GetPlayer(vs.VoiceChannel.Guild);
                await player.Move(vs.VoiceChannel);

                var queue = player.Open<string>("tts");

                //Create audio clip
                var audio = await (await _tts.Synthesize(message)).Open();

                //Enqueue to TTS channel
                var playing = await queue.Enqueue(message, audio);

                //Wait for it to finish playing
                await playing;
            }
        }
    }
}
