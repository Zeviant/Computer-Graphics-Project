using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

[RequireComponent(typeof(AudioListener))]
public class AudioCapture : MonoBehaviour
{
    private List<float> _audioBuffer = new List<float>();
    private bool _isRecording = false;
    private int _channels;
    private int _sampleRate;

	void Update() {
		if (!_isRecording)
			StartCapture();
	}

	void OnDisable() {
		if (_isRecording) {
			StopCapture("/Users/aldo/out.wav");
		}
	}

    public void StartCapture()
    {
        _audioBuffer.Clear();
        _channels = 0;
        _sampleRate = AudioSettings.outputSampleRate;
        _isRecording = true;
    }

    public string StopCapture(string outputPath)
    {
        _isRecording = false;
        SaveToWav(outputPath);
        return outputPath;
    }

    // Called on the audio thread every ~20ms — captures all game audio
    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!_isRecording) return;
        _channels = channels;
        _audioBuffer.AddRange(data);
    }

    private void SaveToWav(string path)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int sampleCount = _audioBuffer.Count;
            int byteCount = sampleCount * 2; // 16-bit PCM

            // WAV Header
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + byteCount);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);           // PCM chunk size
            bw.Write((short)1);     // PCM format
            bw.Write((short)_channels);
            bw.Write(_sampleRate);
            bw.Write(_sampleRate * _channels * 2); // byte rate
            bw.Write((short)(_channels * 2));       // block align
            bw.Write((short)16);    // bits per sample
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(byteCount);

            // Audio data — convert float to 16-bit PCM
            foreach (float sample in _audioBuffer)
            {
                short s = (short)Mathf.Clamp(sample * 32767f, -32768f, 32767f);
                bw.Write(s);
            }
        }
    }
}
