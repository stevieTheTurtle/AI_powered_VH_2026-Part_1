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
        // ===== LIVE-5a · Network ▸ Voice · download the clip =====
        // Connects the reply to the voice, step 1: if r.audio_url is set,
        //   yield return speech.Download(Resolve(r.audio_url), c => clip = c);
        // otherwise TTS failed on the server: log a warning (the body can still react).
        // ===== end LIVE-5a =====

        // ===== TURN-7 · Brain ▸ Body · the face and the gesture start with the voice =====
        // Right before the voice starts: set the emotion (expression) and fire the gesture
        // (behavior) from the reply, so face, gesture and voice start at the same moment.
        // Both fields are empty until body language is wired: check for null.
        // ===== end TURN-7 =====

        // ===== LIVE-5b · Voice ▸ Body · speak =====
        // Step 2: if there is a clip, yield return speech.Play(clip) (it waits until the voice ends).
        // No clip but a body (behavior set): hold the pose for 1.5 s instead.
        // ===== end LIVE-5b =====

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
        // ===== TURN-3 · UI ▸ Network ▸ Body · one typed turn =====
        // Connect the chat box to the server and the server's reply to the body:
        // ask client.Chat(text, onOk, onError) and keep what comes back (reply or error),
        // then: a reply -> yield return Handle(reply); an error -> yield return Fail(error).
        // ===== end TURN-3 =====
        Busy = false;
        yield break;
    }
}
