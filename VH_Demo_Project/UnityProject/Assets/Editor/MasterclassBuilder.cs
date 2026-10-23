using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// VH Demo Project: builds one Unity scene per stage of Day 1 and Day 2 (the stages of the slides),
// plus 00_Index, a menu with one button per stage. Everything comes from Assets/Masterclass/MasterclassConfig.asset:
// project settings, the Animator Controllers + Avatar Mask, the scenes.
// Re-run "Masterclass/Build Demo Scenes" after you change the character, the clips or the config.
public static class MasterclassBuilder
{
    const string Root = "Assets/Masterclass";
    const string ConfigPath = Root + "/MasterclassConfig.asset";
    const string Gen = Root + "/Generated";
    const string SettingsDir = "Assets/Settings";   // URP asset + renderer: same place as in the starter project
    const string ScenesDir = "Assets/Scenes";
    const string TmpEssentials = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";

    struct SceneSpec
    {
        public MasterclassStage stage; public string file, title, tag, notes, index;
    }

    // The order of the slides. Hour 4 (brain, Whisper, Piper in the terminal) has no Unity scene: see IndexEntries.
    static readonly SceneSpec[] Specs =
    {
        new SceneSpec { stage = MasterclassStage.Stage, file = "D1_1_Stage",
            title = "Day 1 · 1. Character on the stage",
            index = "Hour 2 · The stage, a camera framing head and chest, and the idle loop in a one-state Animator.",
            notes = "Camera at eye level, field of view 35: head and chest in frame.\nIdle: a full-body breathing loop.\nNo blink, no gaze yet: the face is still frozen." },
        new SceneSpec { stage = MasterclassStage.Blink, file = "D1_2_Blink",
            title = "Day 1 · 2. Blink (LIVE-1)",
            index = "Hour 2 · BlinkController closes the eyelids every 2-6 s, driving the blendshapes found with BlendShapeLister.",
            notes = "A random blink every 2-6 s: 60 ms to close, 100 ms to open.\nThe blink owns the eyelid shapes: no other script writes them.\nRight-click BlendShapeLister on the character → Print blendshapes: the list in the Console." },
        new SceneSpec { stage = MasterclassStage.Gaze, file = "D1_3_Gaze", tag = "m1-h2",
            title = "Day 1 · 3. Gaze",
            index = "Hour 2 · An Animation Rigging rig: chest, neck, head and eyes aim at the camera, with small saccades of the eyes.",
            notes = "Move the camera with the buttons: head and eyes follow it.\nThe eyes jump a little every 1.5-4 s (saccades); the head follows lazily.\nGaze off: the idle clip drives the head again." },
        new SceneSpec { stage = MasterclassStage.Speaking, file = "D1_4_Speaking",
            title = "Day 1 · 4. Speaking layer (LIVE-2)",
            index = "Hour 3 · Idle below, Speaking on top: an upper-body layer of co-speech beats, switched by CharacterBehaviorManager.SetSpeaking.",
            notes = "Toggle Speaking: arms and torso talk, the legs keep the idle (UpperBodyMask).\nCharacterBehaviorManager is the only door to the Animator." },
        new SceneSpec { stage = MasterclassStage.LipSync, file = "D1_5_LipSync",
            title = "Day 1 · 5. Lip-sync",
            index = "Hour 3 · AudioSource → OVRLipSyncContext (15 visemes) → VisemeDriver (your mouth blendshapes + jaw).",
            notes = "Play the sample clip: the mouth follows the audio; the body stays in idle.\nWithout the OVRLipSync plugin the mouth only opens with the loudness (fallback).\nOne owner per shape: the mouth belongs to the lip-sync." },
        new SceneSpec { stage = MasterclassStage.SpeechPlayer, file = "D1_6_SpeechPlayer", tag = "m1-end",
            title = "Day 1 · 6. Voice and body (TURN-1)",
            index = "Hour 3 · SpeechPlayer.Play: the clip plays with the lips in sync and the Speaking layer on for exactly its length.",
            notes = "Play the sample clip: lips, talk loop and gaze together.\nThe end of Day 1: the body is ready, it just needs something to say." },
        new SceneSpec { stage = MasterclassStage.TypedChat, file = "D2_1_TypedChat", tag = "m2-h2",
            title = "Day 2 · 7. Typed chat over the bridge (TURN-3)",
            index = "Hour 5 · AgentNetworkClient POST /chat → FastAPI: brain (one LLM call) + Piper voice → CharacterDispatcher downloads the WAV, then plays it.",
            notes = "The Python server starts by itself on Play, in its own window (the first start takes a while).\nThe Ollama app must be running and signed in; without it the brain echoes your words.\nType a message, press Enter." },
        new SceneSpec { stage = MasterclassStage.Microphone, file = "D2_2_Microphone",
            title = "Day 2 · 8. Microphone and upload",
            index = "Hour 6 · MicRecorder: push-to-talk at 16 kHz, a WAV in memory, uploaded to the server's /stt-test to read what Whisper heard.",
            notes = "Click the Game view, hold Space, speak, release.\nYou hear your recording back, and the transcript appears below.\nThe typed chat still works." },
        new SceneSpec { stage = MasterclassStage.VoiceLoop, file = "D2_3_VoiceLoop", tag = "m2-h3",
            title = "Day 2 · 9. Voice loop (LIVE-6, TURN-4)",
            index = "Hour 6 · Push-to-talk → /voice-chat: Whisper → one LLM call (text only) → Piper → lip-sync. Timings shown.",
            notes = "Click the Game view, hold Space, speak, release.\nWrite down the three timings: STT · brain · TTS.\nThe body stays neutral: gestures and emotions arrive in the webinar." },
    };

    // The index: the scenes above, with Hour 4 as a note between Day 1 and Day 2.
    static SceneIndexMenu.Entry[] IndexEntries()
    {
        var list = Specs.Select(s => new SceneIndexMenu.Entry { scene = s.file, label = s.title, detail = s.index }).ToList();
        int day2 = Array.FindIndex(Specs, s => s.stage == MasterclassStage.TypedChat);
        list.Insert(day2, new SceneIndexMenu.Entry
        {
            scene = "", label = "Day 2 · Hour 4: the brain, the ears and the voice, in Python only",
            detail = "No Unity scene. In a terminal, in VH_Demo_Project/PythonProject with the .venv active:  python brain.py  ·  python stt.py <your recording>  ·  python tts.py \"Hello\"  ·  python try_pipeline.py <your recording>"
        });
        return list.ToArray();
    }

    // ------------------------------------------------------------------ menu

    [MenuItem("Masterclass/Build Demo Scenes", priority = 0)]
    public static void BuildEverything()
    {
        SetupProject();
        var cfg = LoadOrCreateConfig();
        AutoFill(cfg);
        if (cfg.configureClipImport) { ConfigureClipImport(cfg); AutoFill(cfg); }
        var ctrls = BuildControllers(cfg);
        BuildScenes(cfg, ctrls);
        EditorSceneManager.OpenScene($"{ScenesDir}/00_Index.unity");
        Debug.Log("Masterclass: build complete. 00_Index is open: press Play and pick a stage.");
    }

    [MenuItem("Masterclass/Select Config", priority = 20)]
    static void SelectConfig() { Selection.activeObject = LoadOrCreateConfig(); EditorGUIUtility.PingObject(Selection.activeObject); }

    [MenuItem("Masterclass/Steps/1. Setup Project (URP, HTTP, TMP)")]
    public static void SetupProject()
    {
        EnsureFolder(Gen);
        EnsureFolder(SettingsDir);
        PlayerSettings.insecureHttpOption = InsecureHttpOption.AlwaysAllowed;   // http://127.0.0.1:8000

        if (GraphicsSettings.defaultRenderPipeline == null)
        {
            var rd = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rd, SettingsDir + "/URP_Renderer.asset");
            var urp = UniversalRenderPipelineAsset.Create(rd);
            AssetDatabase.CreateAsset(urp, SettingsDir + "/URP_Asset.asset");
            GraphicsSettings.defaultRenderPipeline = urp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = urp;
            }
            QualitySettings.SetQualityLevel(current, false);
            Debug.Log("Masterclass: URP asset created and assigned.");
        }

        if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro") && File.Exists(Path.GetFullPath(TmpEssentials)))
        {
            AssetDatabase.ImportPackage(TmpEssentials, false);
            Debug.Log("Masterclass: TMP Essential Resources imported.");
        }
        AssetDatabase.SaveAssets();
    }

    [MenuItem("Masterclass/Steps/2. Auto-fill Config from Assets folders")]
    static void AutoFillMenu() { var c = LoadOrCreateConfig(); AutoFill(c); SelectConfig(); }

    [MenuItem("Masterclass/Steps/3. Configure Clip Import Settings")]
    static void ConfigureClipsMenu() { var c = LoadOrCreateConfig(); AutoFill(c); ConfigureClipImport(c); AutoFill(c); }

    [MenuItem("Masterclass/Steps/4. Build Animator Controllers")]
    static void BuildControllersMenu() { var c = LoadOrCreateConfig(); AutoFill(c); BuildControllers(c); }

    [MenuItem("Masterclass/Steps/5. Build Scenes")]
    static void BuildScenesMenu()
    {
        var c = LoadOrCreateConfig(); AutoFill(c);
        BuildScenes(c, BuildControllers(c));
    }

    [MenuItem("Masterclass/Print Face Blendshapes")]
    static void PrintBlendshapes()
    {
        var cfg = LoadOrCreateConfig(); AutoFill(cfg);
        if (cfg.characterPrefab == null) { Debug.LogWarning("No character in the config."); return; }
        var faces = FindFaces(cfg.characterPrefab, cfg.faceRendererNames);
        if (faces.Length == 0) { Debug.LogWarning("No SkinnedMeshRenderer with blendshapes."); return; }
        foreach (var face in faces)
        {
            var m = face.sharedMesh;
            var lines = Enumerable.Range(0, m.blendShapeCount).Select(i => $"{i}: {m.GetBlendShapeName(i)}");
            Debug.Log($"Face mesh '{face.name}' ({m.blendShapeCount} blendshapes):\n" + string.Join("\n", lines));
        }
    }

    // Batch mode:  Unity.exe -batchmode -projectPath <p> -executeMethod MasterclassBuilder.BatchSetup -quit
    public static void BatchSetup() => SetupProject();
    public static void BatchBuild() => BuildEverything();

    // ------------------------------------------------------------------ config

    static MasterclassConfig LoadOrCreateConfig()
    {
        var cfg = AssetDatabase.LoadAssetAtPath<MasterclassConfig>(ConfigPath);
        if (cfg != null) return cfg;
        EnsureFolder(Root);
        cfg = ScriptableObject.CreateInstance<MasterclassConfig>();
        AssetDatabase.CreateAsset(cfg, ConfigPath);
        AssetDatabase.SaveAssets();
        return cfg;
    }

    static void AutoFill(MasterclassConfig cfg)
    {
        if (cfg.characterPrefab == null && AssetDatabase.IsValidFolder("Assets/Characters"))
        {
            cfg.characterPrefab = AssetDatabase.FindAssets("t:GameObject", new[] { "Assets/Characters" })
                .Select(g => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(go => go != null && go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Any())
                .OrderByDescending(go => AssetDatabase.GetAssetPath(go).EndsWith(".prefab"))   // the importer's prefab (cloth, hair) over the raw FBX
                .ThenByDescending(go => go.GetComponentsInChildren<SkinnedMeshRenderer>(true).Max(r => r.sharedMesh ? r.sharedMesh.blendShapeCount : 0))
                .FirstOrDefault();
        }

        // A match in the preferred folder (the Motifect clips) wins; otherwise fill empty slots from Assets/Animations.
        var clips = AllClips("Assets/Animations");
        var preferred = string.IsNullOrEmpty(cfg.preferredClipFolder) ? new List<(AnimationClip, string)>() : AllClips(cfg.preferredClipFolder);
        AnimationClip Slot(AnimationClip current, params string[] words) => Pick(preferred, words) ?? Or(current, Pick(clips, words));
        cfg.idle = Slot(cfg.idle, "idle");
        if (cfg.idle != null && NormName(cfg.idle).Contains("beat")) cfg.idle = null;   // a talk beat is not an idle
        cfg.talk = Slot(cfg.talk, "talk", "speak");
        cfg.wave = Slot(cfg.wave, "wave", "waving");
        cfg.shrug = Slot(cfg.shrug, "shrug");
        cfg.thinking = Slot(cfg.thinking, "think");
        // Co-speech beats (USC VH Toolkit "..._BeatLowBt01"): the Speaking layer cycles through them.
        var beats = clips.Select(c => c.Item1).Where(c => NormName(c).Contains("beat")).Distinct().OrderBy(c => c.name).ToList();
        if (cfg.talk == null && beats.Count > 0) cfg.talk = beats[0];
        if (beats.Count > 0 && (cfg.talkBeats == null || cfg.talkBeats.Count == 0 || cfg.talkBeats.Any(c => c == null))) cfg.talkBeats = beats;

        if (cfg.sampleSpeech == null && AssetDatabase.IsValidFolder("Assets/Audio"))
        {
            var audio = AssetDatabase.FindAssets("t:AudioClip", new[] { "Assets/Audio" }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            var path = audio.FirstOrDefault(p => Path.GetFileName(p).ToLower().Contains("sample")) ?? audio.FirstOrDefault();
            if (path != null) cfg.sampleSpeech = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        EditorUtility.SetDirty(cfg);
        AssetDatabase.SaveAssets();

        var missing = new List<string>();
        if (cfg.characterPrefab == null) missing.Add("character (Assets/Characters)");
        foreach (var (n, c) in new[] { ("idle", cfg.idle), ("talk", cfg.talk) })
            if (c == null) missing.Add(n + " clip");
        if (cfg.sampleSpeech == null) missing.Add("sample speech (Assets/Audio)");
        if (missing.Count > 0) Debug.LogWarning("Masterclass config: missing " + string.Join(", ", missing) + ". Scenes are built anyway: import what is missing and run Masterclass → Build Demo Scenes again.", cfg);
    }

    static List<(AnimationClip clip, string key)> AllClips(string folder)
    {
        var result = new List<(AnimationClip, string)>();
        if (!AssetDatabase.IsValidFolder(folder)) return result;
        foreach (var path in AssetDatabase.FindAssets("t:AnimationClip", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).Distinct())
            foreach (var clip in AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>())
                if (!clip.name.StartsWith("__preview__"))
                    result.Add((clip, (Path.GetFileNameWithoutExtension(path) + " " + clip.name).ToLowerInvariant().Replace(" ", "")));
        return result;
    }

    // Multi-clip packs ("Armature|Idle_Loop", "Armature|Crouch_Idle_Loop", ...): prefer the clip whose own
    // name is the word, then starts with it, then contains it; ties -> the shortest name.
    static AnimationClip Pick(List<(AnimationClip clip, string key)> clips, params string[] words)
    {
        string Norm(AnimationClip c) => NormName(c);
        foreach (var w in words)
        {
            var hit = clips
                .Select(c => (c.clip, n: Norm(c.clip), c.key))
                .Select(c => (c.clip, c.n, score: c.n == w ? 0 : c.n.StartsWith(w) ? 1 : c.n.Contains(w) ? 2 : c.key.Contains(w) ? 3 : 9))
                .Where(c => c.score < 9)
                .OrderBy(c => c.score).ThenBy(c => c.n.Length)
                .FirstOrDefault();
            if (hit.clip != null) return hit.clip;
        }
        return null;
    }

    static string NormName(AnimationClip c)
    {
        string n = c.name.ToLowerInvariant();
        int bar = n.LastIndexOf('|');
        if (bar >= 0) n = n.Substring(bar + 1);
        return n.Replace("_loop", "").Replace("_", "").Replace(" ", "");
    }

    // Hour 3 (T+2:05): Humanoid, loop only idle/talk, bake root motion into pose (gesture clips too, for the webinar).
    // "Copy From Other Avatar" only works when the clip file has the character's bone names;
    // clips on another skeleton (USC VHToolkit, Quaternius, Mixamo) keep their own Humanoid avatar and are retargeted.
    static void ConfigureClipImport(MasterclassConfig cfg)
    {
        Avatar avatar = null;
        if (cfg.characterPrefab != null)
        {
            var anim = cfg.characterPrefab.GetComponentInChildren<Animator>();
            avatar = anim != null && anim.avatar != null ? anim.avatar
                   : AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(cfg.characterPrefab)).OfType<Avatar>().FirstOrDefault();
        }
        var looping = new HashSet<AnimationClip> { cfg.idle, cfg.talk };
        var all = new[] { cfg.idle, cfg.talk, cfg.wave, cfg.shrug, cfg.thinking }
            .Concat(cfg.talkBeats ?? new List<AnimationClip>()).Where(c => c != null).Distinct();

        foreach (var group in all.GroupBy(AssetDatabase.GetAssetPath))
        {
            if (!(AssetImporter.GetAtPath(group.Key) is ModelImporter mi)) continue;   // .anim files: nothing to import
            if (mi.animationType != ModelImporterAnimationType.Human)
            {
                mi.animationType = ModelImporterAnimationType.Human;
                if (avatar != null && avatar.isHuman && AssetDatabase.GetAssetPath(avatar) != group.Key && SameSkeleton(avatar, group.Key))
                {
                    mi.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                    mi.sourceAvatar = avatar;
                }
            }
            var names = new HashSet<string>(group.Select(c => c.name));
            var loopNames = new HashSet<string>(group.Where(looping.Contains).Select(c => c.name));
            var clipAnims = (mi.clipAnimations.Length > 0 ? mi.clipAnimations : mi.defaultClipAnimations);
            foreach (var ca in clipAnims)
            {
                if (!names.Contains(ca.name) && clipAnims.Length > 1) continue;
                ca.loopTime = loopNames.Contains(ca.name) || (clipAnims.Length == 1 && loopNames.Count > 0);
                ca.lockRootRotation = true;      // Bake Into Pose: rotation
                ca.lockRootHeightY = true;       //                 position Y
                ca.lockRootPositionXZ = true;    //                 position XZ
                // Based Upon Body Orientation / Center of Mass, not Original: packs authored facing
                // another axis (Quaternius UAL, Blender exports) would turn the character 180° on Play.
                ca.keepOriginalOrientation = false;
                ca.keepOriginalPositionY = true;      // feet stay on the floor
                ca.keepOriginalPositionXZ = false;
            }
            mi.clipAnimations = clipAnims;
            mi.SaveAndReimport();
        }
    }

    static bool SameSkeleton(Avatar characterAvatar, string clipFile)
    {
        var hips = characterAvatar.humanDescription.human.FirstOrDefault(h => h.humanName == "Hips").boneName;
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(clipFile);
        return !string.IsNullOrEmpty(hips) && root != null
            && root.GetComponentsInChildren<Transform>(true).Any(t => t.name == hips);
    }

    // ------------------------------------------------------------------ animator

    struct Controllers { public AnimatorController h2, body; }

    static Controllers BuildControllers(MasterclassConfig cfg)
    {
        EnsureFolder(Gen);

        // Hour 2: one Idle state.
        string h2Path = Gen + "/VH_Controller_H2.controller";
        AssetDatabase.DeleteAsset(h2Path);
        var h2 = AnimatorController.CreateAnimatorControllerAtPath(h2Path);
        var h2Idle = h2.layers[0].stateMachine.AddState("Idle");
        h2Idle.motion = cfg.idle;
        h2.layers[0].stateMachine.defaultState = h2Idle;

        // Upper-body mask: everything except the root, legs and feet. Idle (base layer) stays full body;
        // talking plays only on the upper body, over it.
        string maskPath = Gen + "/UpperBodyMask.mask";
        AssetDatabase.DeleteAsset(maskPath);
        var mask = new AvatarMask();
        foreach (AvatarMaskBodyPart p in Enum.GetValues(typeof(AvatarMaskBodyPart)))
        {
            if (p == AvatarMaskBodyPart.LastBodyPart) continue;
            bool off = p == AvatarMaskBodyPart.Root || p == AvatarMaskBodyPart.LeftLeg || p == AvatarMaskBodyPart.RightLeg
                    || p == AvatarMaskBodyPart.LeftFootIK || p == AvatarMaskBodyPart.RightFootIK;
            mask.SetHumanoidBodyPartActive(p, !off);
        }
        AssetDatabase.CreateAsset(mask, maskPath);

        // Hour 3 on: base layer = full-body Idle; "Speaking" layer (upper-body mask) = Empty <-> talk beats.
        var body = BaseController(cfg, Gen + "/VH_Controller.controller", mask);
        AssetDatabase.SaveAssets();
        return new Controllers { h2 = h2, body = body };
    }

    // Hour 3: base layer = Idle (full body). "Speaking" layer with the upper-body mask: Empty <-> talk,
    // switched by the bool `Speaking` (no exit time, 0.2 s blend). The gaze is an Animation Rigging rig: no IK Pass needed.
    static AnimatorController BaseController(MasterclassConfig cfg, string path, AvatarMask upperBody)
    {
        AssetDatabase.DeleteAsset(path);
        var c = AnimatorController.CreateAnimatorControllerAtPath(path);
        c.AddParameter("Speaking", AnimatorControllerParameterType.Bool);

        var sm = c.layers[0].stateMachine;
        var idle = sm.AddState("Idle", new Vector3(250, 100));
        idle.motion = cfg.idle;
        sm.defaultState = idle;

        c.AddLayer("Speaking");
        var layers = c.layers;
        layers[1].defaultWeight = 1f;
        layers[1].blendingMode = AnimatorLayerBlendingMode.Override;
        layers[1].avatarMask = upperBody;
        c.layers = layers;

        var tsm = c.layers[1].stateMachine;
        var empty = tsm.AddState("Empty", new Vector3(250, 100));
        tsm.defaultState = empty;

        // Speaking: one looping talk clip (the handout), or a cycle of co-speech beats when the config has them.
        var beats = (cfg.talkBeats ?? new List<AnimationClip>()).Where(b => b != null).ToList();
        var speakStates = new List<AnimatorState>();
        if (beats.Count == 0)
        {
            var speaking = tsm.AddState("Speaking", new Vector3(250, 220));
            speaking.motion = cfg.talk;
            speakStates.Add(speaking);
        }
        else
        {
            for (int i = 0; i < beats.Count; i++)
            {
                var s = tsm.AddState($"Speaking_{beats[i].name}", new Vector3(520, 40 + i * 70));
                s.motion = beats[i];
                speakStates.Add(s);
            }
            for (int i = 0; i < speakStates.Count; i++)       // beat i -> beat i+1 near its end
            {
                var next = speakStates[i].AddTransition(speakStates[(i + 1) % speakStates.Count]);
                next.hasExitTime = true; next.exitTime = 0.85f; next.duration = 0.3f;
            }
        }
        var toSpeak = empty.AddTransition(speakStates[0]);
        toSpeak.hasExitTime = false; toSpeak.duration = 0.2f;
        toSpeak.AddCondition(AnimatorConditionMode.If, 0, "Speaking");
        foreach (var s in speakStates)
        {
            var toEmpty = s.AddTransition(empty);
            toEmpty.hasExitTime = false; toEmpty.duration = 0.25f;
            toEmpty.AddCondition(AnimatorConditionMode.IfNot, 0, "Speaking");
        }
        return c;
    }

    // ------------------------------------------------------------------ scenes

    static void BuildScenes(MasterclassConfig cfg, Controllers ctrls)
    {
        EnsureFolder(ScenesDir);
        var mats = Materials();
        CC4MaterialConverter.BeginBuild();
        var buildList = new List<EditorBuildSettingsScene>();

        string indexPath = $"{ScenesDir}/00_Index.unity";
        BuildIndexScene(indexPath);
        buildList.Add(new EditorBuildSettingsScene(indexPath, true));

        foreach (var spec in Specs)
        {
            string p = $"{ScenesDir}/{spec.file}.unity";
            var ctrl = spec.stage < MasterclassStage.Speaking ? ctrls.h2 : ctrls.body;
            BuildStageScene(cfg, spec, ctrl, mats);
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), p);
            buildList.Add(new EditorBuildSettingsScene(p, true));
        }
        EditorBuildSettings.scenes = buildList.ToArray();
        AssetDatabase.SaveAssets();
    }

    static void BuildIndexScene(string path)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var cam = new GameObject("Main Camera", typeof(Camera)).GetComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.12f, 0.13f, 0.16f);
        var menu = new GameObject("SceneIndex").AddComponent<SceneIndexMenu>();
        Set(menu, "entries", IndexEntries());
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), path);
    }

    static void BuildStageScene(MasterclassConfig cfg, SceneSpec spec, AnimatorController controller, (Material floor, Material wall) mats)
    {
        var st = spec.stage;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Stage (Hour 2, T+1:35): floor, back wall at z = 2, key + fill light.
        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor"; floor.transform.localScale = new Vector3(0.6f, 1, 0.6f);
        floor.GetComponent<Renderer>().sharedMaterial = mats.floor;
        var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = "BackWall"; wall.transform.position = new Vector3(0, 1.5f, 2f); wall.transform.localScale = new Vector3(6, 3, 0.2f);
        wall.GetComponent<Renderer>().sharedMaterial = mats.wall;

        var key = new GameObject("Key Light").AddComponent<Light>();
        key.type = LightType.Directional; key.shadows = LightShadows.Soft; key.intensity = 1.1f;
        key.transform.rotation = Quaternion.Euler(40, 150, 0);
        var fill = new GameObject("Fill Light").AddComponent<Light>();
        fill.type = LightType.Point; fill.intensity = 1.5f; fill.range = 6f;
        fill.transform.position = new Vector3(-1.2f, 1.8f, -1.2f);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.45f, 0.5f);

        // Character at the origin, facing the camera (Y = 180).
        GameObject character; Animator animator; SkinnedMeshRenderer[] faces;
        if (cfg.characterPrefab != null)
        {
            character = (GameObject)PrefabUtility.InstantiatePrefab(cfg.characterPrefab);
            character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180, 0));
            RemoveMissingScripts(character);
            if (cfg.convertMaterialsToURP) CC4MaterialConverter.ApplyToInstance(character);
            animator = character.GetComponentInChildren<Animator>();
            if (animator == null) animator = character.AddComponent<Animator>();
            faces = FindFaces(character, cfg.faceRendererNames);
        }
        else
        {
            character = Placeholder();
            animator = character.AddComponent<Animator>();
            faces = new SkinnedMeshRenderer[0];
        }
        animator.applyRootMotion = false;
        animator.runtimeAnimatorController = controller;
        var root = animator.gameObject;

        // Camera (T+1:35): eye level, ~1.6 m back, FOV 35, head and chest in frame.
        float eyeY = 1.55f;
        var head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        if (head != null) eyeY = head.position.y + 0.07f;
        var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        cam.tag = "MainCamera"; cam.fieldOfView = 35f;
        cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = new Color(0.2f, 0.22f, 0.26f);
        cam.transform.position = new Vector3(0, eyeY, -1.6f);
        cam.transform.LookAt(new Vector3(0, eyeY - 0.25f, 0));

        // Blink: blendshape lister and blink on the root, driving every face mesh.
        if (st >= MasterclassStage.Blink && faces.Length > 0)
        {
            Set(root.AddComponent<BlendShapeLister>(), "faces", faces);
            var blink = root.AddComponent<BlinkController>();
            Set(blink, "faces", faces);
            Set(blink, "blinkShapesContain", cfg.blinkShapesContain);
        }

        GazeController gaze = null; CharacterBehaviorManager behavior = null; AudioSource src = null; SpeechPlayer speech = null;
        AgentNetworkClient client = null; CharacterDispatcher dispatcher = null; MicRecorder mic = null;
        VoiceChatController voice = null; ChatUI chat = null;

        // Gaze: Animation Rigging rig on the root, aimed at the camera.
        if (st >= MasterclassStage.Gaze) gaze = BuildGaze(animator, cam.transform);

        // Speaking layer: the behavior door (SetSpeaking).
        if (st >= MasterclassStage.Speaking)
        {
            behavior = root.AddComponent<CharacterBehaviorManager>();
            Set(behavior, "animator", animator);
        }

        // Lip-sync: the voice comes from the mouth, a 3D AudioSource under the head bone.
        // OVRLipSyncContext must sit next to it (it reads that AudioSource's samples).
        if (st >= MasterclassStage.LipSync)
        {
            var mouth = BuildMouth(animator, root);
            src = mouth.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 1f;                            // 3D: heard from the mouth's position
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 2f; src.maxDistance = 30f;      // full volume at conversation distance
            // OVRLipSyncContext analyses the audio; VisemeDriver maps its 15 visemes onto the face.
            var visemes = root.AddComponent<VisemeDriver>();
            Set(visemes, "source", src); Set(visemes, "faces", faces);
            Set(visemes, "lipSyncContext", AddOvrLipSyncContext(mouth, src));
            Set(visemes, "visemes", cfg.visemes.Select(Clone).ToList());
            Set(visemes, "jaw", FindJaw(animator, cfg.jawBoneName));
            Set(visemes, "jawMaxAngle", cfg.jawMaxAngle);
        }

        // Voice and body: the SpeechPlayer plays a clip and keeps the Speaking layer on meanwhile.
        if (st >= MasterclassStage.SpeechPlayer)
        {
            speech = root.AddComponent<SpeechPlayer>();
            Set(speech, "source", src); Set(speech, "behavior", behavior); Set(speech, "testClip", cfg.sampleSpeech);
        }

        // Day 2: the Network object (client), the dispatcher on the character, the chat box.
        if (st >= MasterclassStage.TypedChat)
        {
            var network = new GameObject("Network");
            client = network.AddComponent<AgentNetworkClient>();
            Set(client, "baseUrl", cfg.serverUrl);
            dispatcher = root.AddComponent<CharacterDispatcher>();
            Set(dispatcher, "client", client); Set(dispatcher, "speech", speech);
            chat = BuildChatUI(dispatcher);

            // Microphone (and, in the voice loop, push-to-talk with a listening cue) on the Network object.
            if (st >= MasterclassStage.Microphone) mic = network.AddComponent<MicRecorder>();
            if (st >= MasterclassStage.VoiceLoop)
            {
                voice = network.AddComponent<VoiceChatController>();
                Set(voice, "mic", mic); Set(voice, "client", client); Set(voice, "dispatcher", dispatcher);
                Set(voice, "listeningCue", BuildListeningCue(chat));
                Set(voice, "chatUI", chat);
            }
        }

        var hud = new GameObject("MasterclassHUD").AddComponent<MasterclassHUD>();
        Set(hud, "stage", st); Set(hud, "title", spec.title); Set(hud, "notes", spec.notes); Set(hud, "checkpointTag", spec.tag ?? "");
        Set(hud, "cam", cam); Set(hud, "gaze", gaze); Set(hud, "behavior", behavior); Set(hud, "mouthSource", src);
        Set(hud, "speech", speech); Set(hud, "sampleSpeech", cfg.sampleSpeech); Set(hud, "client", client);
        Set(hud, "dispatcher", dispatcher); Set(hud, "mic", mic); Set(hud, "voice", voice); Set(hud, "chatUI", chat);
    }

    // OVRLipSync is optional: wired by reflection so the project compiles without the plugin.
    // Only the context (audio analysis) is used: VisemeDriver replaces OVRLipSyncContextMorphTarget.
    static Component AddOvrLipSyncContext(GameObject root, AudioSource src)
    {
        Type manager = FindType("OVRLipSync"), context = FindType("OVRLipSyncContext");
        if (manager == null || context == null) return null;

        if (UnityEngine.Object.FindFirstObjectByType(manager) == null)
            new GameObject("LipSyncInterface").AddComponent(manager);

        var ctx = root.AddComponent(context);
        Set(ctx, "audioSource", src);
        // OVRLipSync 29 silences the AudioSource unless "Audio Loopback" is on (it is meant for mic input).
        Set(ctx, "audioLoopback", true, optional: true);
        return ctx;
    }

    // A child of the head bone, placed in front of the teeth (centre of the teeth mesh), or a fixed offset from the head.
    // Hour 3 gaze: an Animation Rigging rig on the character root, the setup the students build by hand.
    //   root: Animator + RigBuilder + GazeController
    //   └─ GazeRig (Rig): HeadTarget, EyeTarget, Multi-Aim on chest/neck/head (→ HeadTarget) and eyes (→ EyeTarget)
    // Constraints run after all Animator layers, so the gaze survives the talk loop and the gestures.
    static GazeController BuildGaze(Animator animator, Transform cam)
    {
        var root = animator.gameObject;
        var rigGo = new GameObject("GazeRig");
        rigGo.transform.SetParent(root.transform, false);
        var rig = rigGo.AddComponent<Rig>();
        var builder = root.GetComponent<RigBuilder>() ?? root.AddComponent<RigBuilder>();
        builder.layers = new List<RigLayer> { new RigLayer(rig, true) };

        Transform headTarget = new GameObject("HeadTarget").transform, eyeTarget = new GameObject("EyeTarget").transform;
        headTarget.SetParent(rigGo.transform, false); eyeTarget.SetParent(rigGo.transform, false);
        headTarget.position = eyeTarget.position = cam.position;

        var log = new System.Text.StringBuilder();
        Vector3 fwd = root.transform.forward;    // the character faces the camera
        void Aim(string name, HumanBodyBones bone, Transform src, float w)
        {
            Transform t = animator.isHuman ? animator.GetBoneTransform(bone) : null;
            if (t == null) { log.AppendLine($"{name}: bone {bone} not mapped, skipped"); return; }
            var go = new GameObject(name);
            go.transform.SetParent(rigGo.transform, false);
            var c = go.AddComponent<MultiAimConstraint>();
            var d = c.data;
            d.constrainedObject = t;
            var sources = new WeightedTransformArray(0); sources.Add(new WeightedTransform(src, 1f));
            d.sourceObjects = sources;
            d.aimAxis = AimAxisOf(t, fwd); d.upAxis = UpAxisOf(t, Vector3.up);
            d.worldUpType = MultiAimConstraintData.WorldUpType.SceneUp;
            d.constrainedXAxis = d.constrainedYAxis = d.constrainedZAxis = true;
            d.maintainOffset = false;
            c.data = d; c.weight = w;
            log.AppendLine($"{name}: {t.name}  aim {d.aimAxis}  up {d.upAxis}  weight {w}");
        }
        var chest = animator.GetBoneTransform(HumanBodyBones.UpperChest) != null ? HumanBodyBones.UpperChest : HumanBodyBones.Chest;
        Aim("Aim Chest", chest, headTarget, 0.15f);
        Aim("Aim Neck", HumanBodyBones.Neck, headTarget, 0.3f);
        Aim("Aim Head", HumanBodyBones.Head, headTarget, 0.8f);
        Aim("Aim Eye L", HumanBodyBones.LeftEye, eyeTarget, 1f);
        Aim("Aim Eye R", HumanBodyBones.RightEye, eyeTarget, 1f);

        var gaze = root.AddComponent<GazeController>();
        Set(gaze, "target", cam); Set(gaze, "gazeRig", rig);
        Set(gaze, "headTarget", headTarget); Set(gaze, "eyeTarget", eyeTarget);
        File.WriteAllText(Gen + "/GazeRig_axes.txt", log.ToString());
        return gaze;
    }

    // The bone-local axis (±X/±Y/±Z) that points closest to a world direction: CC4, Mixamo and
    // Rocketbox bones all use different local axes, so the aim axis is read from the bind pose.
    static MultiAimConstraintData.Axis AimAxisOf(Transform t, Vector3 worldDir) => AxisOf(t.InverseTransformDirection(worldDir));
    static MultiAimConstraintData.Axis UpAxisOf(Transform t, Vector3 worldDir) => AxisOf(t.InverseTransformDirection(worldDir));
    static MultiAimConstraintData.Axis AxisOf(Vector3 v)
    {
        float ax = Mathf.Abs(v.x), ay = Mathf.Abs(v.y), az = Mathf.Abs(v.z);
        if (ax >= ay && ax >= az) return v.x >= 0 ? MultiAimConstraintData.Axis.X : MultiAimConstraintData.Axis.X_NEG;
        if (ay >= az) return v.y >= 0 ? MultiAimConstraintData.Axis.Y : MultiAimConstraintData.Axis.Y_NEG;
        return v.z >= 0 ? MultiAimConstraintData.Axis.Z : MultiAimConstraintData.Axis.Z_NEG;
    }

    static GameObject BuildMouth(Animator animator, GameObject root)
    {
        var head = animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : null;
        var parent = head != null ? head : root.transform;
        var mouth = new GameObject("Mouth");
        mouth.transform.SetParent(parent, false);

        Vector3 forward = root.transform.forward;
        var teeth = root.GetComponentsInChildren<Renderer>(true).FirstOrDefault(r => r.name.ToLowerInvariant().Contains("teeth"));
        Vector3 pos = teeth != null ? teeth.bounds.center + forward * 0.02f
                    : head != null ? head.position + forward * 0.09f - Vector3.up * 0.06f
                    : root.transform.position + Vector3.up * 1.55f + forward * 0.1f;
        mouth.transform.SetPositionAndRotation(pos, root.transform.rotation);
        return mouth;
    }

    static Transform FindJaw(Animator animator, string name)
    {
        if (!string.IsNullOrEmpty(name))
        {
            var t = animator.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == name);
            if (t != null) return t;
            Debug.LogWarning($"Jaw bone '{name}' not found");
        }
        return animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Jaw) : null;
    }

    static ChatUI BuildChatUI(CharacterDispatcher dispatcher)
    {
        var canvasGO = new GameObject("ChatCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var res = new TMP_DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
        };

        var inputGO = TMP_DefaultControls.CreateInputField(res);
        inputGO.name = "MessageInput";
        inputGO.transform.SetParent(canvasGO.transform, false);
        var irt = (RectTransform)inputGO.transform;
        irt.anchorMin = irt.anchorMax = irt.pivot = new Vector2(0.5f, 0f);
        irt.sizeDelta = new Vector2(820, 56); irt.anchoredPosition = new Vector2(-70, 30);
        var input = inputGO.GetComponent<TMP_InputField>();
        input.pointSize = 24;
        if (input.placeholder is TextMeshProUGUI ph) ph.text = "Type a message and press Enter…";

        var btnGO = TMP_DefaultControls.CreateButton(res);
        btnGO.name = "SendButton";
        btnGO.transform.SetParent(canvasGO.transform, false);
        var brt = (RectTransform)btnGO.transform;
        brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(0.5f, 0f);
        brt.sizeDelta = new Vector2(130, 56); brt.anchoredPosition = new Vector2(415, 30);
        var label = btnGO.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null) { label.text = "Send"; label.fontSize = 24; }

        var ui = canvasGO.AddComponent<ChatUI>();
        Set(ui, "input", input); Set(ui, "sendButton", btnGO.GetComponent<Button>()); Set(ui, "dispatcher", dispatcher);
        return ui;
    }

    // Voice loop: a small red "Listening…" label right of the Send button, hidden until push-to-talk.
    static GameObject BuildListeningCue(ChatUI chat)
    {
        var go = new GameObject("ListeningCue", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(chat.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f); rt.pivot = new Vector2(0f, 0f);
        rt.sizeDelta = new Vector2(260, 56); rt.anchoredPosition = new Vector2(500, 30);   // the Send button ends at x = 480
        var text = go.GetComponent<TextMeshProUGUI>();
        text.text = "● Listening…"; text.fontSize = 28;
        text.horizontalAlignment = HorizontalAlignmentOptions.Left; text.verticalAlignment = VerticalAlignmentOptions.Middle;
        text.color = new Color(1f, 0.35f, 0.35f);
        go.SetActive(false);
        return go;
    }

    // ------------------------------------------------------------------ helpers

    static SkinnedMeshRenderer[] FindFaces(GameObject go, string[] names)
    {
        var all = go.GetComponentsInChildren<SkinnedMeshRenderer>(true)
            .Where(r => r.sharedMesh != null && r.sharedMesh.blendShapeCount > 0).ToList();
        if (names != null && names.Any(n => !string.IsNullOrEmpty(n)))
            all = all.Where(r => names.Any(n => !string.IsNullOrEmpty(n) && r.name.Contains(n))).ToList();
        return all.ToArray();
    }

    // Character packages often carry their importer's scripts (e.g. Reallusion's): drop the missing ones.
    static void RemoveMissingScripts(GameObject go)
    {
        int missing = go.GetComponentsInChildren<Transform>(true)
            .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
        if (missing == 0) return;
        if (PrefabUtility.IsPartOfPrefabInstance(go))
            PrefabUtility.UnpackPrefabInstance(go, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        foreach (var t in go.GetComponentsInChildren<Transform>(true))
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);
        Debug.Log($"Masterclass: removed {missing} missing script(s) from the character instance.");
    }

    static GameObject Placeholder()
    {
        var root = new GameObject("CharacterPlaceholder (put your character in Assets/Characters and rebuild)");
        var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.transform.SetParent(root.transform); body.transform.localPosition = new Vector3(0, 0.75f, 0); body.transform.localScale = new Vector3(0.45f, 0.75f, 0.3f);
        var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        head.transform.SetParent(root.transform); head.transform.localPosition = new Vector3(0, 1.62f, 0); head.transform.localScale = Vector3.one * 0.24f;
        root.transform.rotation = Quaternion.Euler(0, 180, 0);
        return root;
    }

    static (Material, Material) Materials()
    {
        EnsureFolder(Gen);
        return (Mat("Floor", new Color(0.55f, 0.53f, 0.5f)), Mat("Wall", new Color(0.72f, 0.74f, 0.78f)));
    }

    static Material Mat(string name, Color color)
    {
        string p = $"{Gen}/{name}.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, p);
        }
        m.SetColor("_BaseColor", color); m.color = color;
        EditorUtility.SetDirty(m);
        return m;
    }

    static VisemeMix Clone(VisemeMix m) => new VisemeMix
    {
        viseme = m.viseme, jaw = m.jaw,
        shapes = m.shapes.Select(x => new ShapeWeight { shapeContains = x.shapeContains, weight = x.weight }).ToList()
    };

    static EmotionPreset Clone(EmotionPreset p) => new EmotionPreset
    {
        id = p.id,
        shapes = p.shapes.Select(s => new ShapeWeight { shapeContains = s.shapeContains, weight = s.weight }).ToList()
    };

    static T Or<T>(T a, T b) where T : UnityEngine.Object => a != null ? a : b;

    static Type FindType(string name) =>
        AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType(name, false)).FirstOrDefault(t => t != null);

    // Sets a (possibly private, possibly inherited) serialized field, so the course scripts keep
    // the exact [SerializeField] fields of the script handouts.
    static void Set(object target, string field, object value, bool optional = false)
    {
        if (target == null) return;
        for (var t = target.GetType(); t != null; t = t.BaseType)
        {
            var f = t.GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            if (f == null) continue;
            f.SetValue(target, value);
            if (target is UnityEngine.Object u) EditorUtility.SetDirty(u);
            return;
        }
        if (!optional) Debug.LogWarning($"Masterclass: field '{field}' not found on {target.GetType().Name}");
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}
