using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

// Voice ▸ Body: plays a clip through the mouth's AudioSource (lip-sync reads it)
// and tells the body it is speaking.
public class SpeechPlayer : MonoBehaviour
{
    [SerializeField] AudioSource source;
    [SerializeField] CharacterBehaviorManager behavior;
    [SerializeField] AudioClip testClip;

    // Network: download a clip from a URL. Returns it through the callback (null on failure).
    public IEnumerator Download(string url, System.Action<AudioClip> onDone, AudioType type = AudioType.WAV)
    {
        using (var req = UnityWebRequestMultimedia.GetAudioClip(url, type))
        {
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Audio download failed: {req.error}");
                onDone?.Invoke(null);
                yield break;
            }
            onDone?.Invoke(DownloadHandlerAudioClip.GetContent(req));
        }
    }

    public IEnumerator Play(AudioClip clip)
    {
        if (clip == null) yield break;
        // ===== TURN-1 · Voice ▸ Body · speak with the whole body =====
        // Connect the voice to the body: the clip plays on `source` (the lip-sync listens to it)
        // and, for exactly as long as it plays, the body is in Speaking.
        // You need: source.clip, source.Play(), behavior.SetSpeaking(...),
        //           WaitForSeconds(clip.length + 0.15f)  (isPlaying is unreliable when the editor loses focus).
        // ===== end TURN-1 =====
    }

    [ContextMenu("Test/Play test clip")]
    void TestPlay() => StartCoroutine(Play(testClip));
}
