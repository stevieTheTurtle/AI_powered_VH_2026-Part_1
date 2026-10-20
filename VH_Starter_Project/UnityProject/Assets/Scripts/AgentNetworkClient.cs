using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

// The JSON contract: speech_text + audio_url; transcript and timings for the voice loop; action and emotion for body language.
// JsonUtility: [Serializable] classes, public fields,
// unknown fields ignored, no dictionaries (hence the Timings class).
[Serializable] public class ChatRequest  { public string text; }
[Serializable] public class Timings { public int stt, brain, tts; }
[Serializable] public class ChatResponse
{
    public string speech_text;
    public string audio_url;
    public string transcript;
    public Timings timings_ms;
    public string action;
    public string emotion;
    public string brain;       // "echo", "text" or "structured" (shown in the HUD)
}

// Network ▸ HTTP client: send a request, return the response or an error.
public class AgentNetworkClient : MonoBehaviour
{
    // 127.0.0.1, not localhost (avoids IPv6 resolution surprises).
    // Player Settings -> Allow downloads over HTTP = Always allowed.
    [SerializeField] string baseUrl = "http://127.0.0.1:8000";
    [SerializeField] int timeoutSeconds = 30;

    public string BaseUrl => baseUrl;

    public IEnumerator Chat(string text, Action<ChatResponse> onOk, Action<string> onError)
    {
        // Not UnityWebRequest.Post(url, string): that sends URL-encoded form data, not JSON.
        string json = JsonUtility.ToJson(new ChatRequest { text = text });
        using (var req = new UnityWebRequest(baseUrl + "/chat", "POST"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSeconds;
            yield return Send(req, onOk, onError);
        }
    }

    // upload the recorded WAV as multipart form data.
    public IEnumerator VoiceChat(byte[] wav, Action<ChatResponse> onOk, Action<string> onError)
    {
        var form = new WWWForm();
        form.AddBinaryData("audio", wav, "speech.wav", "audio/wav");
        using (var req = UnityWebRequest.Post(baseUrl + "/voice-chat", form))
        {
            req.timeout = timeoutSeconds;
            yield return Send(req, onOk, onError);
        }
    }

    IEnumerator Send(UnityWebRequest req, Action<ChatResponse> onOk, Action<string> onError)
    {
        yield return req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(req.error);
            yield break;
        }

        ChatResponse response = null;
        try { response = JsonUtility.FromJson<ChatResponse>(req.downloadHandler.text); }
        catch (Exception e) { onError?.Invoke("Bad JSON: " + e.Message); yield break; }

        if (response == null) { onError?.Invoke("Empty response"); yield break; }
        onOk?.Invoke(response);
    }
}
