using System;
using NAudio.Wave;
using UnityEngine;

namespace ResourcefulHands;

public class StreamedAudioClip
{
    public AudioClip clip {get; private set;}
    public string path {get; private set;}
    private int _position = 0;
    private int _sampleRate;
    private int _channels;
    private float[] _samples = [];
    
    public StreamedAudioClip(string fileName)
    {
        path = fileName;

        int sampleCount = 0;
        using (var reader = new AudioFileReader(path))
        {
            _sampleRate = reader.WaveFormat.SampleRate;
            _channels = reader.WaveFormat.Channels;
    
            sampleCount = (int)(reader.Length / sizeof(float));
        }

        int lengthSamples = sampleCount / _channels;
        clip = AudioClip.Create("MemoryStreamedAudioClip", lengthSamples, _channels, _sampleRate, true, OnAudioRead, OnAudioSetPosition);
        
        if (RHConfig.LoadFullAudio?.Value ?? false)
        {
            RHLog.Debug($"Loading full audio file for {fileName}");
            LoadFile();
        }
    }

    public void LoadFile()
    {
        var samples = MiscUtils.LoadAudioFile(path, out int sampleRate, out int channels);
        _sampleRate = sampleRate;
        _channels = channels;
        _samples = samples;
    }
    
    void OnAudioRead(float[] data)
    {
        if (_samples.Length == 0)
            LoadFile();
        
        int count = data.Length;
        int available = _samples.Length - _position;

        // Copy only as much as we have
        int samplesToCopy = Mathf.Min(count, available);

        // Copy audio data
        Array.Copy(_samples, _position, data, 0, samplesToCopy);

        // Fill remaining with silence if we run out of data
        for (int i = samplesToCopy; i < count; i++)
        {
            data[i] = 0f;
        }

        _position += samplesToCopy;
    }

    void OnAudioSetPosition(int newPosition)
    {
        _position = newPosition;
    }
}