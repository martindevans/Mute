namespace Mute.Moe.Services.Audio.Mixing.Channels;

public interface IMixerChannel
    : IMixerInput
{
    void Stop();
}