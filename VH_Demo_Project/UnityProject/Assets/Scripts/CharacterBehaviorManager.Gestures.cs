using System.Collections.Generic;
using UnityEngine;

// Body ▸ Animator ▸ Gesture layer: the door gets a second handle.
// LLM output is untrusted input: anything not in the whitelist is logged and ignored.
public partial class CharacterBehaviorManager
{
    // LLM-facing id -> Animator trigger. Anything not in this table is rejected.
    static readonly Dictionary<string, int> Gestures = new Dictionary<string, int>
    {
        { "WAVE",     Animator.StringToHash("Wave") },
        { "SHRUG",    Animator.StringToHash("Shrug") },
        { "THINKING", Animator.StringToHash("Thinking") },
    };

    public static IEnumerable<string> GestureIds => Gestures.Keys;

    public void PlayGesture(string gestureId)
    {
        // LIVE-7
        if (string.IsNullOrWhiteSpace(gestureId)) return;
        string id = gestureId.Trim().ToUpperInvariant();
        if (id == "NONE") return;

        if (!Gestures.TryGetValue(id, out int trigger))
        {
            Debug.LogWarning($"Unknown gesture '{gestureId}' ignored", this);
            return;
        }
        animator.SetTrigger(trigger);
    }

    [ContextMenu("Test/Wave")]     void TestWave()     => PlayGesture("WAVE");
    [ContextMenu("Test/Shrug")]    void TestShrug()    => PlayGesture("SHRUG");
    [ContextMenu("Test/Thinking")] void TestThinking() => PlayGesture("THINKING");
    [ContextMenu("Test/Nonsense (must warn, not crash)")] void TestBad() => PlayGesture("DANCE!");
}
