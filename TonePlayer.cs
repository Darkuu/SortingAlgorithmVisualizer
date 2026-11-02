using System;
using NAudio.Wave;

namespace SortingVisualizer;

public enum ToneWaveform
{
    Sine,
    Triangle,
    Square,
    Sawtooth
}

public sealed class TonePlayer : IDisposable
{
    private readonly ToneProvider _provider = new();
    private readonly WaveOut _output;

    private bool _disposed;

    public TonePlayer()
    {
        _output = new WaveOut();

        try
        {
            _output.Init(_provider);
            _output.Play();
        }
        catch
        {
            _output.Dispose();
            throw;
        }
    }

    public ToneWaveform Waveform
    {
        get => _provider.Waveform;
        set => _provider.Waveform = value;
    }

    public double PitchMultiplier
    {
        get => _provider.PitchMultiplier;
        set => _provider.PitchMultiplier =
            (float)Math.Clamp(value, 0.15, 1.25);
    }

    public void PlayValue(int value, int maximum)
    {
        if (_disposed)
            return;

        double fraction = maximum <= 1
            ? 0
            : Math.Clamp(
                (value - 1.0) / (maximum - 1.0),
                0,
                1);

        const double minimumFrequency = 180;
        const double maximumFrequency = 1400;

        double frequency =
            minimumFrequency *
            Math.Pow(
                maximumFrequency / minimumFrequency,
                fraction);

        _provider.Frequency = (float)frequency;

        // Quiet output, especially for square and sawtooth waves.
        _provider.TargetAmplitude = 0.06f;
    }

    public void Mute()
    {
        _provider.TargetAmplitude = 0;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        _provider.TargetAmplitude = 0;

        try
        {
            _output.Stop();
        }
        finally
        {
            _output.Dispose();
        }
    }

    private sealed class ToneProvider : ISampleProvider
    {
        public WaveFormat WaveFormat { get; } =
            WaveFormat.CreateIeeeFloatWaveFormat(44100, 1);

        // Updated by the UI thread and read by the audio thread.
        public volatile float Frequency = 440;
        public volatile float TargetAmplitude;
        public volatile float PitchMultiplier = 0.5f;
        public volatile ToneWaveform Waveform = ToneWaveform.Triangle;

        private double _phase;
        private double _amplitude;
        private double _currentFrequency = 220;

        public int Read(Span<float> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                // Smooth changes to reduce abrupt transitions.
                _amplitude +=
                    (TargetAmplitude - _amplitude) * 0.002;

                double targetFrequency =
                    Frequency * PitchMultiplier;

                _currentFrequency +=
                    (targetFrequency - _currentFrequency) * 0.002;

                double sample = Waveform switch
                {
                    ToneWaveform.Sine =>
                        Math.Sin(_phase),

                    ToneWaveform.Triangle =>
                        (2.0 / Math.PI) *
                        Math.Asin(Math.Sin(_phase)),

                    ToneWaveform.Square =>
                        Math.Sin(_phase) >= 0 ? 1.0 : -1.0,

                    ToneWaveform.Sawtooth =>
                        (_phase / Math.PI) - 1.0,

                    _ => Math.Sin(_phase)
                };

                buffer[i] = (float)(sample * _amplitude);

                _phase +=
                    2 * Math.PI * _currentFrequency /
                    WaveFormat.SampleRate;

                if (_phase >= 2 * Math.PI)
                {
                    _phase -= 2 * Math.PI;
                }
            }

            return buffer.Length;
        }
    }
}