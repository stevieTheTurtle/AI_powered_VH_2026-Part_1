using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

// The stages of Day 1, Day 2 and the webinar, in the order of the slides. Hour 4 is the Python track (no Unity), so it has no scene.
public enum MasterclassStage { Stage, Blink, Gaze, Speaking, LipSync, SpeechPlayer, TypedChat, Microphone, VoiceLoop, Gestures, Emotions, TalkingCharacter }

// Teaching overlay (IMGUI, no assets needed): what this scene shows, buttons to try the parts that exist
// at this stage, and the live status of the server and the voice loop.
// Not part of the course code: students do not write this.
public class MasterclassHUD : MonoBehaviour
{
    [SerializeField] MasterclassStage stage;
    [SerializeField] string title = "";
    [SerializeField, TextArea(3, 10)] string notes = "";
    [SerializeField] string checkpointTag = "";

    [Header("References (filled by the scene builder)")]
    [SerializeField] Camera cam;
    [SerializeField] GazeController gaze;
    [SerializeField] CharacterBehaviorManager behavior;
    [SerializeField] ExpressionController expression;
    [SerializeField] AudioSource mouthSource;
    [SerializeField] SpeechPlayer speech;
    [SerializeField] AudioClip sampleSpeech;
    [SerializeField] AgentNetworkClient client;
    [SerializeField] CharacterDispatcher dispatcher;
    [SerializeField] MicRecorder mic;
    [SerializeField] VoiceChatController voice;
    [SerializeField] ChatUI chatUI;

    [System.Serializable] class SttReply { public string transcript; }

    static readonly string[] GestureKeys = { "WAVE", "SHRUG", "THINKING" };
    static readonly string[] Emotions = { "NEUTRAL", "HAPPY", "SAD" };

    bool collapsed, gazeOn = true;
    string serverStatus = "?";
    Vector3 camHome; Vector3 lookPoint; float camOffset;
    AudioSource playback;                       // Microphone stage: plays your recording back
    string micStatus = "Hold Space in the Game view, speak, release.";
    string micTranscript = "";
    GUIStyle box, small, subtitle;

    bool Has(MasterclassStage s) => stage >= s;

    void Start()
    {
        if (cam != null) { camHome = cam.transform.position; lookPoint = camHome + cam.transform.forward * 1.6f; }
        if (Has(MasterclassStage.TypedChat) && client != null) StartCoroutine(PingLoop());
        if (stage == MasterclassStage.Microphone) { playback = gameObject.AddComponent<AudioSource>(); playback.playOnAwake = false; playback.spatialBlend = 0f; }
    }

    IEnumerator PingLoop()
    {
        while (true)
        {
            using (var req = UnityWebRequest.Get(client.BaseUrl + "/"))
            {
                req.timeout = 2;
                yield return req.SendWebRequest();
                serverStatus = req.result == UnityWebRequest.Result.Success
                    ? "online " + req.downloadHandler.text
                    : "starting or offline (" + req.error + "). The server window opens on Play; the first start takes a while.";
            }
            yield return new WaitForSeconds(3f);
        }
    }

    void Update()
    {
        if (chatUI != null && chatUI.IsTyping) return;
        if (Input.GetKeyDown(KeyCode.Tab)) collapsed = !collapsed;
        if (behavior != null && Input.GetKeyDown(KeyCode.S)) behavior.SetSpeaking(!behavior.IsSpeaking);
        if (Input.GetKeyDown(KeyCode.P)) PlaySample();
        // Webinar: 1-3 gestures, F1-F3 emotions.
        if (Has(MasterclassStage.Gestures) && behavior != null)
            for (int i = 0; i < GestureKeys.Length; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) behavior.PlayGesture(GestureKeys[i]);
        if (expression != null)
            for (int i = 0; i < Emotions.Length; i++)
                if (Input.GetKeyDown(KeyCode.F1 + i)) expression.SetEmotion(Emotions[i]);

        if (stage == MasterclassStage.Microphone && mic != null)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                mic.StartRecording();
                micStatus = mic.IsRecording ? "● Recording… release Space to stop." : "No microphone found.";
            }
            if (Input.GetKeyUp(KeyCode.Space) && mic.IsRecording)
            {
                byte[] wav = mic.StopRecording();
                if (wav == null) { micStatus = "Too short: hold Space a bit longer."; return; }
                var clip = WavToClip(wav);
                micStatus = $"Recorded {clip.length:0.0} s, {wav.Length / 1024} KB WAV at 16 kHz. Playing it back, and sending it to /stt-test…";
                playback.clip = clip; playback.Play();
                StartCoroutine(Transcribe(wav));
            }
        }
    }

    void PlaySample()
    {
        if (sampleSpeech == null) return;
        if (Has(MasterclassStage.SpeechPlayer) && speech != null) StartCoroutine(speech.Play(sampleSpeech));   // lips + Speaking layer
        else if (Has(MasterclassStage.LipSync) && mouthSource != null) { mouthSource.clip = sampleSpeech; mouthSource.Play(); }   // lips only
    }

    // Microphone stage: the same upload as the voice loop, to the server's test endpoint that only transcribes.
    IEnumerator Transcribe(byte[] wav)
    {
        var form = new WWWForm();
        form.AddBinaryData("audio", wav, "speech.wav", "audio/wav");
        using (var req = UnityWebRequest.Post(client.BaseUrl + "/stt-test", form))
        {
            req.timeout = 60;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { micTranscript = "Server error: " + req.error; yield break; }
            var r = JsonUtility.FromJson<SttReply>(req.downloadHandler.text);
            micTranscript = string.IsNullOrEmpty(r?.transcript) ? "(nothing heard)" : r.transcript;
        }
    }

    // 16-bit PCM WAV (what MicRecorder writes) -> AudioClip.
    static AudioClip WavToClip(byte[] wav)
    {
        int channels = System.BitConverter.ToInt16(wav, 22);
        int rate = System.BitConverter.ToInt32(wav, 24);
        int n = (wav.Length - 44) / 2;
        var data = new float[n];
        for (int i = 0; i < n; i++) data[i] = System.BitConverter.ToInt16(wav, 44 + i * 2) / 32768f;
        var clip = AudioClip.Create("recording", n / channels, channels, rate, false);
        clip.SetData(data, 0);
        return clip;
    }

    void InitStyles()
    {
        if (box != null) return;
        box = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, wordWrap = true, fontSize = 13, padding = new RectOffset(10, 10, 8, 8) };
        small = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 12, richText = true };
        subtitle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 20, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        subtitle.normal.textColor = Color.white;
    }

    void OnGUI()
    {
        InitStyles();
        const float w = 340f;
        GUILayout.BeginArea(new Rect(10, 10, w, Screen.height - 20));
        GUILayout.BeginVertical(box);

        GUILayout.BeginHorizontal();
        GUILayout.Label($"<b>{title}</b>", new GUIStyle(small) { fontSize = 15 });
        if (GUILayout.Button(collapsed ? "+" : "–", GUILayout.Width(24))) collapsed = !collapsed;
        GUILayout.EndHorizontal();

        if (!collapsed)
        {
            if (!string.IsNullOrEmpty(notes)) GUILayout.Label(notes, small);
            if (!string.IsNullOrEmpty(checkpointTag)) GUILayout.Label("Checkpoint tag: " + checkpointTag, small);

            if (Has(MasterclassStage.Gaze) && gaze != null) DrawGaze();
            if (Has(MasterclassStage.Speaking) && behavior != null) DrawSpeaking();
            if (Has(MasterclassStage.LipSync)) DrawVoice();
            if (Has(MasterclassStage.Gestures)) DrawGesturesAndFace();
            if (stage == MasterclassStage.Microphone) DrawMic();
            if (Has(MasterclassStage.TypedChat)) DrawBridge();

            GUILayout.Space(6);
            if (GUILayout.Button("◀ All stages")) SceneManager.LoadScene(0);
            GUILayout.Label("Tab: hide/show this panel", small);
        }
        GUILayout.EndVertical();
        GUILayout.EndArea();

        DrawSubtitle();
    }

    void DrawGaze()
    {
        GUILayout.Space(6);
        GUILayout.Label("<b>Gaze</b>: head and eyes follow the camera", small);
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("◀ Move camera")) MoveCamera(-0.5f);
        if (GUILayout.Button("Centre")) MoveCamera(0f);
        if (GUILayout.Button("Move camera ▶")) MoveCamera(0.5f);
        GUILayout.EndHorizontal();
        bool on = GUILayout.Toggle(gazeOn, " Gaze on");
        if (on != gazeOn) { gazeOn = on; gaze.SetWeight(on ? 1f : 0f); }
    }

    void MoveCamera(float x)
    {
        if (cam == null) return;
        camOffset = x;
        cam.transform.position = camHome + new Vector3(camOffset, 0, 0);
        cam.transform.LookAt(lookPoint);
    }

    void DrawSpeaking()
    {
        GUILayout.Space(6);
        GUILayout.Label("<b>Speaking layer</b> (CharacterBehaviorManager.SetSpeaking)", small);
        bool sp = GUILayout.Toggle(behavior.IsSpeaking, " Speaking   (key S)");
        if (sp != behavior.IsSpeaking) behavior.SetSpeaking(sp);
    }

    void DrawVoice()
    {
        GUILayout.Space(6);
        bool full = Has(MasterclassStage.SpeechPlayer);
        GUILayout.Label(full ? "<b>Voice and body</b> (SpeechPlayer.Play: lips + Speaking layer)" : "<b>Lip-sync</b> (AudioSource → OVRLipSync → VisemeDriver: lips only)", small);
        GUI.enabled = sampleSpeech != null;
        if (GUILayout.Button(sampleSpeech != null ? "▶ Play the sample clip   (key P)" : "No sample clip in Assets/Audio")) PlaySample();
        GUI.enabled = true;
    }

    void DrawGesturesAndFace()
    {
        GUILayout.Space(6);
        if (behavior != null)
        {
            GUILayout.Label("<b>Gestures</b> (CharacterBehaviorManager.PlayGesture, whitelist)", small);
            GUILayout.BeginHorizontal();
            foreach (var g in GestureKeys)
                if (GUILayout.Button(g)) behavior.PlayGesture(g);
            GUILayout.EndHorizontal();
            if (GUILayout.Button("\"DANCE!\" (not in the whitelist)")) behavior.PlayGesture("DANCE!");
        }
        if (expression != null)
        {
            GUILayout.Label($"<b>Emotion</b> (ExpressionController): {expression.CurrentEmotion}", small);
            GUILayout.BeginHorizontal();
            foreach (var e in Emotions)
                if (GUILayout.Button(e)) expression.SetEmotion(e);
            GUILayout.EndHorizontal();
        }
        GUILayout.Label(expression != null ? "Keys: 1-3 gestures · F1-F3 emotions" : "Keys: 1-3 gestures", small);
    }

    void DrawMic()
    {
        GUILayout.Space(6);
        GUILayout.Label("<b>Microphone</b> (MicRecorder → WAV in memory → upload)", small);
        GUILayout.Label(micStatus, small);
        if (!string.IsNullOrEmpty(micTranscript)) GUILayout.Label("Whisper heard: <b>" + micTranscript + "</b>", small);
    }

    void DrawBridge()
    {
        GUILayout.Space(6);
        GUILayout.Label("<b>Server</b>: " + serverStatus, small);
        if (voice != null)
            GUILayout.Label($"<b>Voice loop</b>: {voice.State}   (hold {voice.PushToTalk} in the Game view)", small);
        if (dispatcher == null) return;
        if (!string.IsNullOrEmpty(dispatcher.LastError))
            GUILayout.Label("<color=#ff8080>Error: " + dispatcher.LastError + "</color>", small);
        var r = dispatcher.LastReply;
        if (r == null) return;
        if (!string.IsNullOrEmpty(r.transcript)) GUILayout.Label("You said: " + r.transcript, small);
        string body = Has(MasterclassStage.TalkingCharacter) ? $"action {r.action} · emotion {r.emotion}" : "text only (neutral body)";
        GUILayout.Label("Reply: " + body + (string.IsNullOrEmpty(r.brain) ? "" : $" · brain {r.brain}"), small);
        if (r.timings_ms != null)
        {
            var t = r.timings_ms;
            GUILayout.Label($"Timings: STT {t.stt} ms · Brain {t.brain} ms · TTS {t.tts} ms · Σ {t.stt + t.brain + t.tts} ms", small);
        }
    }

    void DrawSubtitle()
    {
        if (dispatcher == null || dispatcher.LastReply == null) return;
        var rect = new Rect(Screen.width * 0.15f, Screen.height - 150, Screen.width * 0.7f, 50);
        var shadow = new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height);
        var c = subtitle.normal.textColor;
        subtitle.normal.textColor = Color.black; GUI.Label(shadow, dispatcher.LastReply.speech_text, subtitle);
        subtitle.normal.textColor = c;           GUI.Label(rect, dispatcher.LastReply.speech_text, subtitle);
    }
}
