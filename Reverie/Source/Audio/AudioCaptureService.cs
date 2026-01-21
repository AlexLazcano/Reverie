using System;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace Reverie.Source.Audio;

public class AudioCaptureService : IDisposable
{
    private WasapiLoopbackCapture _capture;
    private readonly float[] _sampleBuffer;
    private int _sampleBufferIndex;
    private readonly int _fftSize;
    private readonly object _lock = new();
    private bool _isRunning;

    // Smoothing factor (0.1 = smooth, 0.5 = responsive)
    private const float SmoothingFactor = 0.5f;

    // Amplification to make frequency response more visible
    private const float BassGain = 10f;
    private const float MidGain = 30f;
    private const float TrebleGain = 30f;

    // Thread-safe frequency band properties
    private volatile float _bass;
    private volatile float _lowMid;
    private volatile float _mid;
    private volatile float _highMid;
    private volatile float _treble;
    private volatile float _currentLevel;

    // Beat detection
    private const int EnergyHistorySize = 43;  // ~1 second at 48kHz/2048 samples
    private readonly float[] _energyHistory = new float[EnergyHistorySize];
    private int _energyHistoryIndex = 0;
    private volatile bool _isBeat;
    private volatile float _beatIntensity;
    private float _lastBeatTime;
    private float _totalTime;
    private const float BeatCooldown = 0.1f;  // 100ms minimum between beats
    private const float BeatThreshold = 1.4f;  // Current must be 1.4x average to trigger

    public bool IsBeat => _isBeat;
    public float BeatIntensity => _beatIntensity;

    public float Bass => _bass;
    public float LowMid => _lowMid;
    public float Mid => _mid;
    public float HighMid => _highMid;
    public float Treble => _treble;
    public float CurrentLevel => _currentLevel;

    public AudioCaptureService(int fftSize = 2048)
    {
        _fftSize = fftSize;
        _sampleBuffer = new float[fftSize];
        _sampleBufferIndex = 0;
    }

    public void Start()
    {
        if (_isRunning)
            return;

        try
        {
            _capture = new WasapiLoopbackCapture();
            _capture.DataAvailable += OnDataAvailable;
            _capture.RecordingStopped += OnRecordingStopped;
            _capture.StartRecording();
            _isRunning = true;
            Console.WriteLine($"Audio capture started. Format: {_capture.WaveFormat.SampleRate}Hz, {_capture.WaveFormat.Channels} channels");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to start audio capture: {ex.Message}");
        }
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _capture?.StopRecording();
        _isRunning = false;
    }

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {
        if (e.BytesRecorded == 0)
            return;

        var format = _capture.WaveFormat;
        int bytesPerSample = format.BitsPerSample / 8;
        int channels = format.Channels;
        int sampleCount = e.BytesRecorded / bytesPerSample / channels;

        // Calculate RMS level from raw samples
        float sumSquares = 0f;
        int validSamples = 0;

        // Process samples and fill FFT buffer
        for (int i = 0; i < sampleCount; i++)
        {
            float sample = 0f;
            int offset = i * bytesPerSample * channels;

            // Average all channels
            for (int ch = 0; ch < channels; ch++)
            {
                int channelOffset = offset + (ch * bytesPerSample);
                if (channelOffset + bytesPerSample <= e.BytesRecorded)
                {
                    sample += BitConverter.ToSingle(e.Buffer, channelOffset);
                }
            }
            sample /= channels;

            sumSquares += sample * sample;
            validSamples++;

            // Add to circular FFT buffer
            lock (_lock)
            {
                _sampleBuffer[_sampleBufferIndex] = sample;
                _sampleBufferIndex = (_sampleBufferIndex + 1) % _fftSize;

                // When buffer is full, perform FFT analysis
                if (_sampleBufferIndex == 0)
                {
                    PerformFFTAnalysis(format.SampleRate);
                }
            }
        }

        // Update RMS level
        if (validSamples > 0)
        {
            float rms = MathF.Sqrt(sumSquares / validSamples);
            float smoothedLevel = _currentLevel + (rms - _currentLevel) * SmoothingFactor;
            _currentLevel = smoothedLevel;
        }
    }

    private void PerformFFTAnalysis(int sampleRate)
    {
        // Copy buffer to avoid modification during FFT
        double[] fftInput = new double[_fftSize];
        for (int i = 0; i < _fftSize; i++)
        {
            int idx = (_sampleBufferIndex + i) % _fftSize;
            fftInput[i] = _sampleBuffer[idx];
        }

        // Apply Hanning window to reduce spectral leakage
        var window = new FftSharp.Windows.Hanning();
        window.ApplyInPlace(fftInput);

        // Perform FFT and get magnitudes
        System.Numerics.Complex[] spectrum = FftSharp.FFT.Forward(fftInput);
        double[] fftMagnitudes = FftSharp.FFT.Magnitude(spectrum);

        // Calculate frequency bands
        CalculateFrequencyBands(fftMagnitudes, sampleRate);
    }

    private void CalculateFrequencyBands(double[] fftMagnitudes, int sampleRate)
    {
        float binWidth = (float)sampleRate / _fftSize;

        // Convert Hz to bin index
        int BinIndex(float hz) => Math.Clamp((int)(hz / binWidth), 0, fftMagnitudes.Length - 1);

        // Calculate band averages and apply gain
        float newBass = AverageMagnitude(fftMagnitudes, BinIndex(20), BinIndex(250)) * BassGain;
        float newLowMid = AverageMagnitude(fftMagnitudes, BinIndex(250), BinIndex(500)) * MidGain;
        float newMid = AverageMagnitude(fftMagnitudes, BinIndex(500), BinIndex(2000)) * MidGain;
        float newHighMid = AverageMagnitude(fftMagnitudes, BinIndex(2000), BinIndex(4000)) * TrebleGain;
        float newTreble = AverageMagnitude(fftMagnitudes, BinIndex(4000), BinIndex(16000)) * TrebleGain;

        // Clamp to 0-1 range
        newBass = Math.Clamp(newBass, 0f, 1f);
        newLowMid = Math.Clamp(newLowMid, 0f, 1f);
        newMid = Math.Clamp(newMid, 0f, 1f);
        newHighMid = Math.Clamp(newHighMid, 0f, 1f);
        newTreble = Math.Clamp(newTreble, 0f, 1f);

        // Apply exponential smoothing
        _bass = _bass + (newBass - _bass) * SmoothingFactor;
        _lowMid = _lowMid + (newLowMid - _lowMid) * SmoothingFactor;
        _mid = _mid + (newMid - _mid) * SmoothingFactor;
        _highMid = _highMid + (newHighMid - _highMid) * SmoothingFactor;
        _treble = _treble + (newTreble - _treble) * SmoothingFactor;

        // Detect beats using bass energy
        DetectBeat(_bass);
    }

    private void DetectBeat(float currentBass)
    {
        // Estimate time progression based on FFT rate (~23ms per FFT at 48kHz/2048)
        _totalTime += (float)_fftSize / 48000f;

        // Add to history
        _energyHistory[_energyHistoryIndex] = currentBass;
        _energyHistoryIndex = (_energyHistoryIndex + 1) % EnergyHistorySize;

        // Calculate average energy
        float avgEnergy = 0f;
        for (int i = 0; i < EnergyHistorySize; i++)
            avgEnergy += _energyHistory[i];
        avgEnergy /= EnergyHistorySize;

        // Check for beat (current significantly above average + cooldown passed)
        bool cooldownPassed = (_totalTime - _lastBeatTime) > BeatCooldown;

        if (currentBass > avgEnergy * BeatThreshold && avgEnergy > 0.05f && cooldownPassed)
        {
            _isBeat = true;
            _beatIntensity = Math.Clamp((currentBass - avgEnergy) / avgEnergy, 0f, 1f);
            _lastBeatTime = _totalTime;
        }
        else
        {
            _isBeat = false;
            _beatIntensity *= 0.9f;  // Decay
        }
    }

    private static float AverageMagnitude(double[] magnitudes, int startBin, int endBin)
    {
        if (startBin >= endBin || startBin >= magnitudes.Length)
            return 0f;

        endBin = Math.Min(endBin, magnitudes.Length);

        double sum = 0;
        for (int i = startBin; i < endBin; i++)
        {
            sum += magnitudes[i];
        }

        return (float)(sum / (endBin - startBin));
    }

    private void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
        if (e.Exception != null)
        {
            Console.WriteLine($"Audio capture stopped with error: {e.Exception.Message}");
        }
        else
        {
            Console.WriteLine("Audio capture stopped.");
        }
    }

    public void Dispose()
    {
        Stop();
        _capture?.Dispose();
        _capture = null;
    }
}
