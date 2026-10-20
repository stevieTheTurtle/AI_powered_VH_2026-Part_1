using TMPro;
using UnityEngine;
using UnityEngine.UI;

// UI ▸ typed chat: input field + Send button -> CharacterDispatcher.Say. Enter also sends.
public class ChatUI : MonoBehaviour
{
    [SerializeField] TMP_InputField input;
    [SerializeField] Button sendButton;
    [SerializeField] CharacterDispatcher dispatcher;

    void Start()
    {
        if (sendButton != null) sendButton.onClick.AddListener(Send);
        if (input != null) input.onSubmit.AddListener(_ => Send());
    }

    public void Send()
    {
        if (input == null || string.IsNullOrWhiteSpace(input.text)) return;
        dispatcher.Say(input.text);
        input.text = "";
        input.ActivateInputField();
    }

    // Grays the field and the button out (while the voice turn runs).
    public void SetInteractable(bool on)
    {
        if (input != null)
        {
            input.interactable = on;
            if (!on) input.DeactivateInputField();
        }
        if (sendButton != null) sendButton.interactable = on;
    }

    // True while the user is typing: the push-to-talk key must not fire then.
    public bool IsTyping => input != null && input.isFocused;
}
