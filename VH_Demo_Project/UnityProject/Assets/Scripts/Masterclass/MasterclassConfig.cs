using System.Collections.Generic;
using UnityEngine;

// The one asset you fill in: which character, which clips, which blendshape names.
// Masterclass -> Build Demo Scenes reads it and regenerates the controllers and the scenes.
// Defaults are for the course character (Reallusion CC4, "CC4 Extended" facial profile).
[CreateAssetMenu(menuName = "Masterclass/Config", fileName = "MasterclassConfig")]
public class MasterclassConfig : ScriptableObject
{
    [Header("Character (Hour 2)")]
    [Tooltip("Character prefab or model (FBX) with a Humanoid rig and face blendshapes.")]
    public GameObject characterPrefab;
    [Tooltip("Face meshes driven by blink, emotion and lip-sync. Empty = every mesh that has blendshapes.")]
    public string[] faceRendererNames = new string[0];
    public string[] blinkShapesContain = { "Eye_Blink" };
    [Tooltip("Rebuild materials whose shader is missing (e.g. CC4 HDRP shaders) on URP Lit, in the generated scenes only.")]
    public bool convertMaterialsToURP = true;

    [Header("Clips: idle (Hour 2), talk (Hour 3), gestures (webinar)")]
    public AnimationClip idle;
    public AnimationClip talk;
    [Tooltip("Optional: Speaking cycles through these co-speech beats (in order) instead of looping the talk clip. "
           + "Auto-filled from clips named Beat* in the preferred folder; empty = the talk clip.")]
    public List<AnimationClip> talkBeats = new List<AnimationClip>();
    public AnimationClip wave, shrug, thinking;
    [Tooltip("On build, a clip found here (by name) replaces the slot (the idle). Empty = only fill empty slots from Assets/Animations.")]
    public string preferredClipFolder = "Assets/Animations/Mixamo";
    [Tooltip("On build: set clips to Humanoid (copy avatar from the character), loop idle/talk, bake root motion.")]
    public bool configureClipImport = true;

    [Header("Lip-sync (Hour 3): 15 Oculus visemes -> your shapes (weights 0-100) + jaw (0-1)")]
    public List<VisemeMix> visemes = CC4Visemes();
    [Tooltip("Jaw bone. Empty = the Humanoid Jaw bone.")]
    public string jawBoneName = "";
    public float jawMaxAngle = 12f;
    public AudioClip sampleSpeech;

    [Header("Emotions (webinar): brows and eyes only")]
    public List<EmotionPreset> emotions = CC4Emotions();

    [Header("Bridge (Hour 5)")]
    public string serverUrl = "http://127.0.0.1:8000";

    // ------------------------------------------------------------------ presets

    static VisemeMix V(string viseme, float jaw, params (string shape, float w)[] shapes)
    {
        var m = new VisemeMix { viseme = viseme, jaw = jaw };
        foreach (var (shape, w) in shapes) m.shapes.Add(new ShapeWeight { shapeContains = shape, weight = w });
        return m;
    }

    static EmotionPreset E(string id, params (string shape, float w)[] shapes)
    {
        var p = new EmotionPreset { id = id };
        foreach (var (shape, w) in shapes) p.shapes.Add(new ShapeWeight { shapeContains = shape, weight = w });
        return p;
    }

    // Many-to-one: 15 Oculus visemes onto CC4's 8 lip shapes + tongue shapes.
    public static List<VisemeMix> CC4Visemes() => new List<VisemeMix>
    {
        V("sil", 0f),
        V("PP", 0f,    ("V_Explosive", 100)),
        V("FF", 0.05f, ("V_Dental_Lip", 100)),
        V("TH", 0.15f, ("V_Tongue_Out", 60), ("V_Open", 20)),
        V("DD", 0.2f,  ("V_Open", 30), ("V_Tongue_Raise", 60)),
        V("kk", 0.3f,  ("V_Open", 40), ("V_Tongue_Lower", 30)),
        V("CH", 0.1f,  ("V_Affricate", 100)),
        V("SS", 0.05f, ("V_Wide", 60)),
        V("nn", 0.15f, ("V_Lip_Open", 50), ("V_Tongue_Raise", 50)),
        V("RR", 0.15f, ("V_Tight_O", 50), ("V_Tongue_Curl_U", 40)),
        V("aa", 1f,    ("V_Open", 100)),
        V("E", 0.45f,  ("V_Wide", 70), ("V_Open", 40)),
        V("ih", 0.25f, ("V_Wide", 60), ("V_Open", 20)),
        V("oh", 0.5f,  ("V_Tight_O", 80), ("V_Open", 30)),
        V("ou", 0.15f, ("V_Tight", 90)),
    };

    // Ready Player Me / Oculus-named characters: one shape per viseme.
    public static List<VisemeMix> OculusNamedVisemes()
    {
        string[] shape = { "", "viseme_PP", "viseme_FF", "viseme_TH", "viseme_DD", "viseme_kk", "viseme_CH", "viseme_SS",
                           "viseme_nn", "viseme_RR", "viseme_aa", "viseme_E", "viseme_I", "viseme_O", "viseme_U" };
        var list = new List<VisemeMix>();
        for (int i = 0; i < 15; i++)
            list.Add(i == 0 ? V("sil", 0) : V(VisemeDriver.OculusVisemes[i], 0, (shape[i], 100)));
        return list;
    }

    public static List<EmotionPreset> CC4Emotions() => new List<EmotionPreset>
    {
        E("HAPPY",     ("Cheek_Raise", 70), ("Brow_Raise_Inner", 25), ("Eye_Squint", 35)),
        E("SAD",       ("Brow_Raise_Inner", 90), ("Brow_Compress", 30), ("Look_Down", 30)),
    };

    public static List<EmotionPreset> ArkitEmotions() => new List<EmotionPreset>
    {
        E("HAPPY",     ("cheekSquint", 70), ("browInnerUp", 25), ("eyeSquint", 35)),
        E("SAD",       ("browInnerUp", 90), ("browDown", 20), ("eyeLookDown", 35)),
    };

    [ContextMenu("Presets/Reallusion CC4 (course character)")]
    void PresetCC4() { visemes = CC4Visemes(); emotions = CC4Emotions(); blinkShapesContain = new[] { "Eye_Blink" }; }

    [ContextMenu("Presets/Ready Player Me (Oculus + ARKit names)")]
    void PresetRpm() { visemes = OculusNamedVisemes(); emotions = ArkitEmotions(); blinkShapesContain = new[] { "eyeBlink" }; jawMaxAngle = 0f; }
}
