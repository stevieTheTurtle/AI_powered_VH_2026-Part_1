using System.Collections;
using UnityEngine;

// Input ▸ Network ▸ Body: hold the key, speak, release -> /voice-chat -> dispatcher.
// The behavior field stays empty until body language is added.
public class VoiceChatController : MonoBehaviour
{
    [SerializeField] KeyCode pushToTalk = KeyCode.Space;
    [SerializeField] MicRecorder mic;
    [SerializeField] AgentNetworkClient client;
    [SerializeField] CharacterDispatcher dispatcher;
    [SerializeField] GameObject listeningCue;          // a small icon, optional
    [SerializeField] CharacterBehaviorManager behavior; // body language: THINKING while waiting
    [SerializeField] ChatUI chatUI;                     // optional: no push-to-talk while typing

    bool busy;

    public string State { get; private set; } = "Idle";
    public KeyCode PushToTalk => pushToTalk;

    void Update()
    {
        if (busy) return;
        if (chatUI != null && chatUI.IsTyping) return;
        if (Input.GetKeyDown(pushToTalk))
        {
            mic.StartRecording();
            State = mic.IsRecording ? "Recording" : "Idle";
            Show(mic.IsRecording);
            LockChat(mic.IsRecording);
        }
        if (Input.GetKeyUp(pushToTalk))
        {
            byte[] wav = mic.StopRecording();
            State = "Idle";
            if (wav != null) StartCoroutine(Run(wav)); else { Show(false); LockChat(false); }
        }
    }

    IEnumerator Run(byte[] wav)
    {
        busy = true;                                    // the cue stays on while the server thinks
        State = "Thinking";
        if (behavior != null) behavior.PlayGesture("THINKING");   // body language: masks the latency

        // LIVE-6
        ChatResponse reply = null; string error = null;
        yield return client.VoiceChat(wav, r => reply = r, e => error = e);
        Show(false);

        if (reply != null)
        {
            Debug.Log($"You said: {reply.transcript}");
            State = "Speaking";
            yield return dispatcher.Handle(reply);
        }
        else
        {
            yield return dispatcher.Fail(error);        // body language: in-character SHRUG
        }

        State = "Idle";
        busy = false;
        LockChat(false);
        yield break;
    }

    void Show(bool on) { if (listeningCue) listeningCue.SetActive(on); }

    // The typed chat is grayed out for the whole voice turn: recording, thinking and speaking.
    void LockChat(bool locked) { if (chatUI != null) chatUI.SetInteractable(!locked); }
}
