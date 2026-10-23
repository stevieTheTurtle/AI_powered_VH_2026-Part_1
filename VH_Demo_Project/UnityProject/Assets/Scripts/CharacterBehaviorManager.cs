using UnityEngine;

// Body ▸ Animator: the character's only door. Everything else (speech, network, brain)
// asks this class for behavior; nobody else touches the Animator.
// Body language adds PlayGesture in CharacterBehaviorManager.Gestures.cs.
public partial class CharacterBehaviorManager : MonoBehaviour
{
    [SerializeField] Animator animator;

    static readonly int SpeakingHash = Animator.StringToHash("Speaking");

    public bool IsSpeaking => animator != null && animator.GetBool(SpeakingHash);

    void Reset() => animator = GetComponent<Animator>();

    public void SetSpeaking(bool speaking)
    {
        // LIVE-2
        animator.SetBool(SpeakingHash, speaking);
    }

    // Debug: right-click the component header while in Play mode.
    [ContextMenu("Test/Speaking On")]  void SpeakOn()  => SetSpeaking(true);
    [ContextMenu("Test/Speaking Off")] void SpeakOff() => SetSpeaking(false);
}
