using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

// One Oculus viseme -> a weighted mix of your face shapes, plus how much the jaw opens.
[Serializable] public class VisemeMix
{
    public string viseme;
    public List<ShapeWeight> shapes = new List<ShapeWeight>();
    [Range(0, 1)] public float jaw;
}

// Body ▸ Face ▸ Lip-sync: replaces OVRLipSyncContextMorphTarget.
// OVRLipSyncContext (same GameObject as the AudioSource) gives 15 viseme weights per frame;
// this script maps them many-to-one onto the character's own shapes on ALL face meshes
// (body, teeth, tongue...) and rotates the jaw bone (CC4 opens the teeth with the bone, not a morph).
// Without the OVRLipSync plugin it falls back to loudness -> "aa".
// Owns the mouth shapes and the jaw (Slide 33).
public class VisemeDriver : MonoBehaviour
{
    public static readonly string[] OculusVisemes =
        { "sil", "PP", "FF", "TH", "DD", "kk", "CH", "SS", "nn", "RR", "aa", "E", "ih", "oh", "ou" };

    [SerializeField] AudioSource source;
    [SerializeField] SkinnedMeshRenderer[] faces;
    [Tooltip("OVRLipSyncContext on the AudioSource object. Empty = loudness fallback.")]
    [SerializeField] Component lipSyncContext;
    [SerializeField] List<VisemeMix> visemes = new List<VisemeMix>();
    [SerializeField, Range(0, 2)] float intensity = 1f;
    [SerializeField] float smoothing = 18f;

    [Header("Jaw bone")]
    [SerializeField] Transform jaw;
    [SerializeField] float jawMaxAngle = 12f;

    [Header("Loudness fallback (no OVRLipSync)")]
    [SerializeField] float gain = 6f;

    readonly List<ShapeRef> slots = new List<ShapeRef>();
    readonly List<(int slot, float weight)>[] mix = new List<(int, float)>[15];
    readonly float[] jawPerViseme = new float[15];
    readonly float[] weights = new float[15];
    readonly float[] samples = new float[256];
    float[] current, target;
    float jawCurrent;

    MethodInfo getFrame; FieldInfo frameVisemes;
    Quaternion jawRest; Vector3 jawAxisLocal; bool hasJaw;

    public float[] VisemeWeights => weights;

    void Start()
    {
        var index = new Dictionary<(SkinnedMeshRenderer, int), int>();
        for (int v = 0; v < 15; v++)
        {
            mix[v] = new List<(int, float)>();
            var m = visemes.Find(x => x.viseme == OculusVisemes[v]);
            if (m == null) continue;
            jawPerViseme[v] = m.jaw;
            foreach (var s in m.shapes)
            {
                var found = FaceShapes.Find(faces, s.shapeContains);
                if (found.Count == 0) Debug.LogWarning($"Viseme {m.viseme}: no shape '{s.shapeContains}'", this);
                foreach (var f in found)
                {
                    if (!index.TryGetValue((f.renderer, f.index), out int slot))
                    {
                        slot = slots.Count; slots.Add(f); index[(f.renderer, f.index)] = slot;
                    }
                    mix[v].Add((slot, s.weight / 100f));
                }
            }
        }
        current = new float[slots.Count];
        target = new float[slots.Count];

        if (lipSyncContext != null)
        {
            getFrame = lipSyncContext.GetType().GetMethod("GetCurrentPhonemeFrame");
            if (getFrame == null) { Debug.LogWarning("VisemeDriver: context has no GetCurrentPhonemeFrame()", this); lipSyncContext = null; }
            else if (!OvrLibraryReady())
            {
                // e.g. Apple Silicon editor: the 29.0.0 macOS library is x86_64 only.
                Debug.LogWarning("VisemeDriver: OVRLipSync native library not initialized, using the loudness fallback", this);
                lipSyncContext = null;
            }
        }
        SetupJaw();
    }

    // OVRLipSync.IsInitialized() == Result.Success (0), read by reflection: the plugin is optional.
    static bool OvrLibraryReady()
    {
        try
        {
            var lib = lipSyncType ??= FindType("OVRLipSync");
            var result = lib?.GetMethod("IsInitialized", BindingFlags.Public | BindingFlags.Static)?.Invoke(null, null);
            return result != null && Convert.ToInt32(result) == 0;
        }
        catch (Exception) { return false; }
    }

    static Type lipSyncType;
    static Type FindType(string name)
    {
        foreach (var a in AppDomain.CurrentDomain.GetAssemblies())
        {
            var t = a.GetType(name, false);
            if (t != null) return t;
        }
        return null;
    }

    // Find the axis that moves the chin down, in the bone's local space, from the pose at start.
    void SetupJaw()
    {
        if (jaw == null || jawMaxAngle <= 0f) return;
        hasJaw = true;
        jawRest = jaw.localRotation;
        var animator = GetComponentInParent<Animator>();
        Transform body = animator != null ? animator.transform : transform;
        Vector3 axisWorld = body.right;
        Vector3 chin = jaw.position + body.forward * 0.1f - body.up * 0.05f;
        Vector3 rotated = jaw.position + Quaternion.AngleAxis(10f, axisWorld) * (chin - jaw.position);
        if (rotated.y > chin.y) axisWorld = -axisWorld;          // rotation must lower the chin
        jawAxisLocal = Quaternion.Inverse(jaw.rotation) * axisWorld;
    }

    void Update()
    {
        ReadWeights();

        float k = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        Array.Clear(target, 0, target.Length);
        float jawTarget = 0f;
        for (int v = 1; v < 15; v++)                 // 0 = sil: the mouth at rest
        {
            float w = weights[v];
            if (w <= 0.001f) continue;
            foreach (var (slot, weight) in mix[v]) target[slot] += w * weight;
            jawTarget += w * jawPerViseme[v];
        }
        for (int i = 0; i < current.Length; i++)
        {
            current[i] = Mathf.Lerp(current[i], Mathf.Clamp01(target[i] * intensity), k);
            slots[i].Set(current[i] * 100f);
        }
        jawCurrent = Mathf.Lerp(jawCurrent, Mathf.Clamp01(jawTarget * intensity), k);
    }

    void ReadWeights()
    {
        Array.Clear(weights, 0, 15);
        if (lipSyncContext != null)
        {
            var frame = getFrame.Invoke(lipSyncContext, null);
            if (frame == null) return;
            if (frameVisemes == null) frameVisemes = frame.GetType().GetField("Visemes");
            if (frameVisemes?.GetValue(frame) is float[] v)
                for (int i = 0; i < Mathf.Min(15, v.Length); i++) weights[i] = v[i];
            return;
        }
        if (source == null || !source.isPlaying) return;
        source.GetOutputData(samples, 0);
        float sum = 0f;
        foreach (var s in samples) sum += s * s;
        weights[10] = Mathf.Clamp01(Mathf.Sqrt(sum / samples.Length) * gain);   // "aa"
    }

    // After the Animator: Humanoid resets the jaw every frame, we open it on top.
    void LateUpdate()
    {
        if (hasJaw) jaw.localRotation = jawRest * Quaternion.AngleAxis(jawCurrent * jawMaxAngle, jawAxisLocal);
    }

    [ContextMenu("Test/Print mapping")]
    void PrintMapping()
    {
        for (int v = 0; v < 15; v++)
            Debug.Log($"{OculusVisemes[v]}: {mix[v]?.Count ?? 0} shape targets, jaw {jawPerViseme[v]:0.00}", this);
    }
}
