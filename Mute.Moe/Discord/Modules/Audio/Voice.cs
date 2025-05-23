﻿using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Mute.Moe.Discord.Attributes;
using Mute.Moe.Services.Speech;
using Mute.Moe.Services.Speech.TTS;
using Mute.Moe.Utilities;

namespace Mute.Moe.Discord.Modules.Audio;

[UsedImplicitly]
public class Voice
    : BaseModule
{
    private readonly IGuildSpeechQueueCollection _queueCollection;
    private readonly ITextToSpeech _tts;

    public Voice(IGuildSpeechQueueCollection queueCollection, ITextToSpeech tts)
    {
        _queueCollection = queueCollection;
        _tts = tts;
    }

    [RequireOwner]
    [Command("tts")]
    [ThinkingReply(EmojiLookup.SpeakerMedVolume)]
    [UsedImplicitly]
    public async Task TextToSpeech([Remainder] string message)
    {
        var user = Context.User;

        if (user is not IVoiceState vs)
        {
            await ReplyAsync("You are not in a voice channel");
        }
        else
        {
            // Get the audio output for this guild and move it to the right channel
            var q = await _queueCollection.Get(Context.Guild.Id);
            await q.VoicePlayer.Move(vs.VoiceChannel);

            // Create audio clip
            var audio = await (await _tts.Synthesize(message)).Open();

            // Enqueue to TTS channel
            var playing = await q.Enqueue(message, audio);

            // Wait for it to finish playing
            await playing;
        }
    }

    //[RequireOwner]
    //[Command("echo")]
    //[ThinkingReply(EmojiLookup.StudioMicrophone)]
    //public async Task Echo()
    //{
    //    var user = Context.User;

    //    if (!(user is IVoiceState vs))
    //    {
    //        await ReplyAsync("You are not in a voice channel");
    //    }
    //    else
    //    {
    //        await Task.Delay(1000);

    //        if (vs.VoiceChannel == null)
    //            return;
    //        if (Context.Guild == null)
    //            return;
    //        if (!(vs is SocketGuildUser sgu))
    //            return;
    //        if (!(sgu.AudioStream is InputStream input))
    //        {
    //            await ReplyAsync("No audio stream");
    //            return;
    //        }

    //        await TextToSpeech("I am now recording you");

    //        var q = await _queueCollection.Get(Context.Guild.Id);
    //        await q.VoicePlayer.Move(vs.VoiceChannel);

    //        var w = input.AsWaveProvider(new WaveFormat(48000, 16, 2));

    //        //Enqueue to TTS channel
    //        var playing = await q.Enqueue("loopback", w.ToSampleProvider());

    //        //Wait for it to finish playing
    //        await playing;
    //    }
    //}

    //[RequireOwner]
    //[Command("stt")]
    //[ThinkingReply(EmojiLookup.StudioMicrophone)]
    //public async Task SpeechToText()
    //{
    //    if (!(Context.User is IVoiceState vs))
    //        return;
    //    if (vs.VoiceChannel == null)
    //        return;
    //    if (Context.Guild == null)
    //        return;
    //    if (!(vs is SocketGuildUser sgu))
    //        return;
    //    if (!(sgu.AudioStream is InputStream input))
    //        return;
    //    await TextToSpeech("I am now recording you");

    //    var c = new CancellationTokenSource();
    //    var w = input.AsWaveProvider(new WaveFormat(48000, 16, 2));

    //    var txt = "Recognising: ";
    //    var msg = await ReplyAsync(txt);

    //    var words = new List<string>();
    //    var timer = new Stopwatch();
    //    timer.Start();

    //    await foreach (var word in _stt.ContinuousRecognition(w, c.Token, null, null))
    //    {
    //        words.Add(word.Text ?? "???");

    //        var stop = word.Text?.Equals("stop", StringComparison.InvariantCultureIgnoreCase) ?? false;

    //        if (timer.ElapsedMilliseconds > 150 || stop)
    //        {
    //            txt += " " + string.Join(" ", words);

    //            await msg.ModifyAsync(a => a.Content = txt);
    //            words.Clear();
    //        }

    //        if (stop)
    //            c.Cancel();
    //    }

    //    await ReplyAsync("Done");
    //}
}