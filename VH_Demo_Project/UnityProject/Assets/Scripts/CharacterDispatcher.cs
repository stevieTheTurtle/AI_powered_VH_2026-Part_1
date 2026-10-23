using System.Collections;
using UnityEngine;

// Network ▸ Body: the realizer's front door. A reply from the server comes in,
// the body performs it. The behavior and expression fields stay empty until body language
// is added, so those lines do nothing before.
public class CharacterDispatcher : MonoBehaviour
{
    [SerializeField] AgentNetworkClient client;
    [SerializeField] SpeechPlayer speech;
    [SerializeField] CharacterBehaviorManager behavior;     // body language (gestures)
    [SerializeField] ExpressionController expression;       // body language (face)

    public ChatResponse LastReply { get; private set; }
    public string LastError { get; private set; }
    public bool Busy { get; private set; }

    public IEnumerator Handle(ChatResponse r)
    {
        LastReply = r;
        LastError = null;
        if (r.timings_ms != null)
            Debug.Log($"[{r.brain}] stt {r.timings_ms.stt} ms | brain {r.timings_ms.brain} ms | tts {r.timings_ms.tts} ms");

        AudioClip clip = null;
        // LIVE-5a
        if (!string.IsNullOrEmpty(r.audio_url))
            yield return speech.Download(Resolve(r.audio_url), c => clip = c);
        else
            Debug.LogWarning("TTS failed: the server sent no audio for this reply. Check the server console.");

        // ===== TURN-7 · Brain ▸ Body · the face and the gesture start with the voice =====
        // Webinar: set the emotion and fire the gesture here, right before the voice starts.
        // ===== end TURN-7 =====

        // LIVE-5b
        if (clip != null) yield return speech.Play(clip);
        else if (behavior != null) yield return new WaitForSeconds(1.5f);   // no audio: hold the pose briefly

        if (expression != null) expression.SetEmotion("NEUTRAL");   // the face goes back after the voice
        yield break;
    }

    // Logs the failure. With body language wired: fail in character, shrug and look sad.
    public IEnumerator Fail(string error)
    {
        LastError = error;
        Debug.LogError("Chat failed: " + error);
        if (behavior == null) yield break;
        if (expression != null) expression.SetEmotion("SAD");
        behavior.PlayGesture("SHRUG");
        yield return new WaitForSeconds(2f);
        if (expression != null) expression.SetEmotion("NEUTRAL");
    }

    // The server returns a relative path ("/static/...").
    string Resolve(string url) =>
        url.StartsWith("http") ? url : client.BaseUrl + url;

    // Called by the chat box.
    public void Say(string text)
    {
        if (Busy || string.IsNullOrWhiteSpace(text)) return;
        StartCoroutine(SayRoutine(text));
    }

    IEnumerator SayRoutine(string text)
    {
        Busy = true;
        // TURN-3
        ChatResponse reply = null; string error = null;
        yield return client.Chat(text, r => reply = r, e => error = e);
        if (reply != null) yield return Handle(reply);
        else yield return Fail(error);
        Busy = false;
        yield break;
    }
}
