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
        // TURN-1
        source.clip = clip;
        source.Play();
        behavior.SetSpeaking(true);
        // Clip length + a small tail: isPlaying breaks when the editor loses focus.
        yield return new WaitForSeconds(clip.length + 0.15f);
        behavior.SetSpeaking(false);
    }

    [ContextMenu("Test/Play test clip")]
    void TestPlay() => StartCoroutine(Play(testClip));
}
