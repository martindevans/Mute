using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Mute.Moe.Services.Audio.Mixing;

/// <summary>
/// Applies automatic gain control to a sample provider
/// </summary>
public class AutoGainControl
    : ISampleProvider
{
    private readonly VolumeSampleProvider _upstream;

    /// <inheritdoc />
    public WaveFormat WaveFormat => _upstream.WaveFormat;

    private readonly double _maxVolume;
    private readonly double _minVolume;
    private readonly double _minGain;
    private readonly double _maxGain;
    private readonly double _upRate;
    private readonly double _downRate;

    private readonly double _secondsPerSample;

    private int _rmsWriteHead;
    private readonly float[] _rmsBuffer;

    /// <summary>
    /// Current measured volume
    /// </summary>
    public float Dbfs { get; private set; }

    /// <summary>
    /// Create a new gain controller
    /// </summary>
    /// <param name="upstream">Upstream audio source</param>
    /// <param name="maxDbfs">Max DBFS before gain is reduced</param>
    /// <param name="minDbfs">Min DBFS before gain is increase</param>
    /// <param name="minGain">Min gain that can be applied</param>
    /// <param name="maxGain">Max gain that can be applied</param>
    /// <param name="upRate">How fast to increase gain</param>
    /// <param name="downRate">How fast to decrease gain</param>
    public AutoGainControl(ISampleProvider upstream, double maxDbfs, double minDbfs, double minGain, double maxGain, double upRate, double downRate)
    {
        _upstream = new VolumeSampleProvider(upstream) { Volume = 1 };
        _maxVolume = maxDbfs;
        _minVolume = minDbfs;
        _minGain = minGain;
        _maxGain = maxGain;
        _upRate = upRate;
        _downRate = downRate;

        _secondsPerSample = 1f / upstream.WaveFormat.SampleRate;
        _rmsBuffer = new float[upstream.WaveFormat.SampleRate];
    }

    /// <inheritdoc />
    public int Read(float[] buffer, int offset, int count)
    {
        // Read requested data from upstream
        var read = _upstream.Read(buffer, offset, count);

        // Write RMS of these samples into RMS buffer
        for (var i = 0; i < read; i++)
        {
            var v = buffer[offset + i];
            _rmsBuffer[_rmsWriteHead++ % _rmsBuffer.Length] = v * v;
        }

        // Calculate RMS over signal
        var rms = MathF.Sqrt(_rmsBuffer.Sum() / _rmsBuffer.Length);
        Dbfs = 20 * MathF.Log10(rms) + 3.0103f;

        // Calculate direction to change gain
        double rate;
        if (Dbfs > _maxVolume)
            rate = -Math.Abs(_downRate);
        else if (Dbfs < _minVolume)
            rate = _upRate;
        else
            return read;

        // Adjust gain
        var gain = _upstream.Volume + rate * _secondsPerSample * read;
        gain = Math.Max(gain, _minGain);
        gain = Math.Min(gain, _maxGain);
        _upstream.Volume = (float)gain;

        return read;
    }
}