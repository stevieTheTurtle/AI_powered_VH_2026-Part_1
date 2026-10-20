using System.IO;
using System.Text;
using UnityEngine;

// Input ▸ Microphone: push-to-talk capture, 16 kHz mono, encoded as WAV in memory.
public class MicRecorder : MonoBehaviour
{
    [SerializeField] int sampleRate = 16000;
    [SerializeField] int maxSeconds = 20;

    AudioClip clip;
    string device;
    public bool IsRecording { get; private set; }

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0) { Debug.LogError("No microphone"); return; }
        device = Microphone.devices[0];
        clip = Microphone.Start(device, false, maxSeconds, sampleRate);
        IsRecording = true;
    }

    // Returns a WAV file (bytes), or null if nothing was recorded.
    public byte[] StopRecording()
    {
        if (!IsRecording) return null;
        IsRecording = false;
        int samples = Microphone.GetPosition(device);
        Microphone.End(device);
        if (samples < sampleRate / 4) return null;     // shorter than 0.25 s: ignore

        var data = new float[samples * clip.channels];
        clip.GetData(data, 0);
        return ToWav(data, clip.channels, sampleRate);
    }

    static byte[] ToWav(float[] samples, int channels, int rate)
    {
        using (var ms = new MemoryStream())
        using (var w = new BinaryWriter(ms))
        {
            int bytes = samples.Length * 2;
            w.Write(Encoding.ASCII.GetBytes("RIFF")); w.Write(36 + bytes);
            w.Write(Encoding.ASCII.GetBytes("WAVE"));
            w.Write(Encoding.ASCII.GetBytes("fmt ")); w.Write(16);
            w.Write((short)1); w.Write((short)channels);
            w.Write(rate); w.Write(rate * channels * 2);
            w.Write((short)(channels * 2)); w.Write((short)16);
            w.Write(Encoding.ASCII.GetBytes("data")); w.Write(bytes);
            foreach (var s in samples)
                w.Write((short)(Mathf.Clamp(s, -1f, 1f) * short.MaxValue));
            w.Flush();
            return ms.ToArray();
        }
    }
}
