using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable] public class ShapeWeight { public string shapeContains; [Range(0, 100)] public float weight; }
[Serializable] public class EmotionPreset { public string id; public List<ShapeWeight> shapes = new List<ShapeWeight>(); }

// Body ▸ Face (emotion): emotion as face. Owns brows and eye shapes only ("Who owns which blendshape?"):
// the mouth belongs to lip-sync, the eyelids to the blink.
public class ExpressionController : MonoBehaviour
{
    [SerializeField] SkinnedMeshRenderer[] faces;
    [SerializeField] List<EmotionPreset> presets = new List<EmotionPreset>();
    [SerializeField] float blendSeconds = 0.25f;

    // Every (mesh, blendshape) any preset touches, resolved once.
    readonly List<ShapeRef> slots = new List<ShapeRef>();
    readonly Dictionary<string, List<(int slot, float weight)>> resolved = new Dictionary<string, List<(int, float)>>();
    float[] current, target;

    public string CurrentEmotion { get; private set; } = "NEUTRAL";

    void Awake()
    {
        if (!FaceShapes.Any(faces)) { Debug.LogWarning("ExpressionController: no face renderer assigned", this); enabled = false; return; }
        var index = new Dictionary<(SkinnedMeshRenderer, int), int>();
        foreach (var p in presets)
        {
            var list = new List<(int, float)>();
            foreach (var s in p.shapes)
            {
                var found = FaceShapes.Find(faces, s.shapeContains);
                if (found.Count == 0) Debug.LogWarning($"Blendshape containing '{s.shapeContains}' not found", this);
                foreach (var f in found)
                {
                    if (!index.TryGetValue((f.renderer, f.index), out int slot))
                    {
                        slot = slots.Count; slots.Add(f); index[(f.renderer, f.index)] = slot;
                    }
                    list.Add((slot, s.weight));
                }
            }
            resolved[p.id.Trim().ToUpperInvariant()] = list;
        }
        current = new float[slots.Count];
        target = new float[slots.Count];
    }

    public void SetEmotion(string emotionId)
    {
        if (target == null) return;
        Array.Clear(target, 0, target.Length);          // go back to neutral
        CurrentEmotion = "NEUTRAL";
        if (string.IsNullOrWhiteSpace(emotionId)) return;

        string id = emotionId.Trim().ToUpperInvariant();
        if (!resolved.TryGetValue(id, out var shapes))
        {
            if (id != "NEUTRAL") Debug.LogWarning($"Unknown emotion '{emotionId}' ignored", this);
            return;
        }
        CurrentEmotion = id;
        foreach (var (slot, weight) in shapes) target[slot] = weight;
    }

    void Update()
    {
        float speed = 100f / Mathf.Max(0.01f, blendSeconds);
        for (int i = 0; i < current.Length; i++)
        {
            if (Mathf.Approximately(current[i], target[i])) continue;
            current[i] = Mathf.MoveTowards(current[i], target[i], speed * Time.deltaTime);
            slots[i].Set(current[i]);
        }
    }

    [ContextMenu("Emotion/Neutral")]   void TestNeutral()   => SetEmotion("NEUTRAL");
    [ContextMenu("Emotion/Happy")]     void TestHappy()     => SetEmotion("HAPPY");
    [ContextMenu("Emotion/Sad")]       void TestSad()       => SetEmotion("SAD");
    [ContextMenu("Emotion/Surprised")] void TestSurprised() => SetEmotion("SURPRISED");
    [ContextMenu("Emotion/Angry")]     void TestAngry()     => SetEmotion("ANGRY");
}
