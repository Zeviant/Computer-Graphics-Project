using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using FFmpegOut;

[RequireComponent(typeof(Camera))]
[RequireComponent(typeof(AudioListener))]
[AddComponentMenu("FFmpegOut/Media Capture")]
public class MediaCapture : MonoBehaviour
{
    #region Serialized fields

    [SerializeField] int _width = 1920;
    [SerializeField] int _height = 1080;
    [SerializeField] FFmpegPreset _preset;
    [SerializeField] float _frameRate = 60;
    [SerializeField] string _outputDirectory = "";

    #endregion

    #region Private members

    FFmpegSession _session;
    RenderTexture _tempRT;
    GameObject _blitter;

    List<float> _audioBuffer = new List<float>();
    bool _isCapturing;
    int _channels;
    int _sampleRate;

    string _videoTempPath;
    string _audioTempPath;
    string _outputPath;

    RenderTextureFormat GetTargetFormat(Camera camera) =>
        camera.allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;

    int GetAntiAliasingLevel(Camera camera) =>
        camera.allowMSAA ? QualitySettings.antiAliasing : 1;

    #endregion

    #region Time-keeping variables

    int _frameCount;
    float _startTime;
    int _frameDropCount;

    float FrameTime => _startTime + (_frameCount - 0.5f) / _frameRate;

    void WarnFrameDrop()
    {
        if (++_frameDropCount != 10) return;
        Debug.LogWarning(
            "Significant frame dropping detected. This may introduce " +
            "time instability into output video. Decreasing the recording " +
            "frame rate is recommended."
        );
    }

    #endregion

    #region Paths

    string OutputDir =>
        string.IsNullOrEmpty(_outputDirectory)
            ? Path.GetFullPath(Application.dataPath + "/..")
            : _outputDirectory;

    string GetAudioCodec() =>
        _preset.GetSuffix() == ".webm" ? "libopus" : "aac";

    #endregion

    #region MonoBehaviour implementation

    void OnValidate()
    {
        _width = Mathf.Max(8, _width);
        _height = Mathf.Max(8, _height);
    }

    void OnDisable()
    {
        if (_session != null)
        {
            _session.Close();
            _session.Dispose();
            _session = null;
        }

        if (_tempRT != null)
        {
            GetComponent<Camera>().targetTexture = null;
            Destroy(_tempRT);
            _tempRT = null;
        }

        if (_blitter != null)
        {
            Destroy(_blitter);
            _blitter = null;
        }

        _isCapturing = false;

        if (_videoTempPath == null) return;

        if (_audioBuffer.Count == 0 || _channels == 0)
        {
            Debug.LogWarning("MediaCapture: no audio data captured — skipping mux.");
            return;
        }

        SaveWav(_audioTempPath);
        MuxVideoAndAudio();
    }

    IEnumerator Start()
    {
        for (var eof = new WaitForEndOfFrame();;)
        {
            yield return eof;
            _session?.CompletePushFrames();
        }
    }

    void Update()
    {
        var camera = GetComponent<Camera>();

        if (_session == null)
        {
            var ts = System.DateTime.Now.ToString("yyyy_MMdd_HHmmss");
            var dir = OutputDir;
            _videoTempPath = Path.Combine(dir, $"temp_video_{ts}.mp4");
            _audioTempPath = Path.Combine(dir, $"temp_audio_{ts}.wav");
            _outputPath    = Path.Combine(dir, $"Recording_{ts}{_preset.GetSuffix()}");

            if (camera.targetTexture == null)
            {
                _tempRT = new RenderTexture(_width, _height, 24, GetTargetFormat(camera));
                _tempRT.antiAliasing = GetAntiAliasingLevel(camera);
                camera.targetTexture = _tempRT;
                _blitter = MediaCaptureBlitter.CreateInstance(camera);
            }

            _session = FFmpegSession.CreateWithOutputPath(
                _videoTempPath,
                camera.targetTexture.width,
                camera.targetTexture.height,
                _frameRate, _preset
            );

            _sampleRate = AudioSettings.outputSampleRate;
            _audioBuffer = new List<float>(_sampleRate * 2 * 60);
            _channels = 0;
            _isCapturing = true;

            _startTime = Time.time;
            _frameCount = 0;
            _frameDropCount = 0;
        }

        var gap = Time.time - FrameTime;
        var delta = 1 / _frameRate;

        if (gap < 0)
        {
            _session.PushFrame(null);
        }
        else if (gap < delta)
        {
            _session.PushFrame(camera.targetTexture);
            _frameCount++;
        }
        else if (gap < delta * 2)
        {
            _session.PushFrame(camera.targetTexture);
            _session.PushFrame(camera.targetTexture);
            _frameCount += 2;
        }
        else
        {
            WarnFrameDrop();
            _session.PushFrame(camera.targetTexture);
            _frameCount += Mathf.FloorToInt(gap * _frameRate);
        }
    }

    void OnAudioFilterRead(float[] data, int channels)
    {
        if (!_isCapturing) return;
        _channels = channels;
        _audioBuffer.AddRange(data);
    }

    #endregion

    #region WAV write

    void SaveWav(string path)
    {
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            int sampleCount = _audioBuffer.Count;
            int byteCount = sampleCount * 2;

            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + byteCount);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1);
            bw.Write((short)_channels);
            bw.Write(_sampleRate);
            bw.Write(_sampleRate * _channels * 2);
            bw.Write((short)(_channels * 2));
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(byteCount);

            foreach (float sample in _audioBuffer)
            {
                short s = (short)Mathf.Clamp(sample * 32767f, -32768f, 32767f);
                bw.Write(s);
            }
        }
    }

    #endregion

    #region Mux

    void MuxVideoAndAudio()
    {
        var args = $"-y -i \"{_videoTempPath}\" -i \"{_audioTempPath}\""
                 + $" -c:v copy -c:a {GetAudioCodec()} \"{_outputPath}\"";

        var proc = System.Diagnostics.Process.Start(
            new System.Diagnostics.ProcessStartInfo {
                FileName = FFmpegPipe.ExecutablePath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true
            }
        );

        proc.WaitForExit();

        var err = proc.StandardError.ReadToEnd();
        if (!string.IsNullOrEmpty(err))
            Debug.Log("FFmpeg mux:\n" + err);

        proc.Close();
        proc.Dispose();

        File.Delete(_videoTempPath);
        File.Delete(_audioTempPath);

        Debug.Log($"MediaCapture: saved to {_outputPath}");
    }

    #endregion
}

// Replicates FFmpegOut.Blitter (which is internal) using the same shader and mesh approach.
// Placed in the same file so MediaCapture can use it without exposing it as a public API.
sealed class MediaCaptureBlitter : MonoBehaviour
{
    const int UILayer = 5;

    Texture _sourceTexture;
    Mesh _mesh;
    Material _material;

    public static GameObject CreateInstance(Camera source)
    {
        var go = new GameObject("Blitter", typeof(Camera), typeof(MediaCaptureBlitter));
        go.hideFlags = HideFlags.HideInHierarchy;

        var camera = go.GetComponent<Camera>();
        camera.cullingMask = 1 << UILayer;
        camera.targetDisplay = source.targetDisplay;

        var blitter = go.GetComponent<MediaCaptureBlitter>();
        blitter._sourceTexture = source.targetTexture;

        return go;
    }

    void PreCull(Camera camera)
    {
        if (_mesh == null || camera != GetComponent<Camera>()) return;
        Graphics.DrawMesh(_mesh, transform.localToWorldMatrix, _material, UILayer, camera);
    }

    void BeginCameraRendering(ScriptableRenderContext context, Camera camera) => PreCull(camera);

    void Update()
    {
        if (_mesh != null) return;

        _mesh = new Mesh();
        _mesh.vertices = new Vector3[3];
        _mesh.triangles = new int[] { 0, 1, 2 };
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
        _mesh.UploadMeshData(true);

        var shader = Shader.Find("Hidden/FFmpegOut/Blitter");
        _material = new Material(shader);
        _material.SetTexture("_MainTex", _sourceTexture);

        RenderPipelineManager.beginCameraRendering += BeginCameraRendering;
        Camera.onPreCull += PreCull;
    }

    void OnDisable()
    {
        if (_mesh == null) return;

        RenderPipelineManager.beginCameraRendering -= BeginCameraRendering;
        Camera.onPreCull -= PreCull;

        Destroy(_mesh);
        Destroy(_material);
        _mesh = null;
        _material = null;
    }
}
