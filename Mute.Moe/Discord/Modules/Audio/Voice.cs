using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Speech;
using Mute.Moe.Services.Speech.TTS;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules.Audio
{
    public class Voice
        : BaseModule
    {
        private readonly ITextToSpeech _tts;
        private readonly IGuildSpeechQueueCollection _queueCollection;

        public Voice(ITextToSpeech tts, IGuildSpeechQueueCollection queueCollection)
        {
            _tts = tts;
            _queueCollection = queueCollection;
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
                var q = await _queueCollection.Get(Context.Guild.Id);
                await q.VoicePlayer.Move(vs.VoiceChannel);

                //Create audio clip
                var audio = await (await _tts.Synthesize(message)).Open();

                //Enqueue to TTS channel
                var playing = await q.Enqueue(message, audio);

                //Wait for it to finish playing
                await playing;
            }
        }
    }
}
