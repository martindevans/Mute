﻿using System.Threading.Tasks;
using Mute.Moe.Services.Audio.Clips;

namespace Mute.Moe.Services.Speech.TTS;

public interface ITextToSpeech
{
    /// <summary>
    /// Convert a string of text to an audio clip ready for playback
    /// </summary>
    /// <param name="text"></param>
    /// <param name="voice">Voice to use (service dependent)</param>
    /// <returns></returns>
    Task<IAudioClip> Synthesize(string text, string? voice  = null);
}