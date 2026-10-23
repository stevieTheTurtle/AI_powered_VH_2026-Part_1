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
        // ===== LIVE-7 · Body ▸ Gesture layer · the whitelist =====
        // Connects the brain's "action" string to an Animator trigger, safely.
        // Write: ignore empty and "NONE"; normalize (Trim, ToUpperInvariant);
        //        look the id up in Gestures: found -> animator.SetTrigger, not found -> Debug.LogWarning.
        // ===== end LIVE-7 =====
    }

    [ContextMenu("Test/Wave")]     void TestWave()     => PlayGesture("WAVE");
    [ContextMenu("Test/Shrug")]    void TestShrug()    => PlayGesture("SHRUG");
    [ContextMenu("Test/Thinking")] void TestThinking() => PlayGesture("THINKING");
    [ContextMenu("Test/Nonsense (must warn, not crash)")] void TestBad() => PlayGesture("DANCE!");
}
