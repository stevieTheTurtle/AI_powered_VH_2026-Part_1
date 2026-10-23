using System.IO;
using System.Linq;
using UnityEditor;

// Builds the demo scenes by itself, once, as soon as the character is in the project:
// on opening the project, or right after importing Camila_Character.unitypackage.
// Without the character it shows what to import. Afterwards: Masterclass → Build Demo Scenes.
[InitializeOnLoad]
static class MasterclassAutoBuild
{
    const string Marker = "Assets/Scenes/00_Index.unity";
    const string BuildStarted = "Masterclass.BuildStarted";
    const string Explained = "Masterclass.Explained";

    static MasterclassAutoBuild()
    {
        if (File.Exists(Marker) || SessionState.GetBool(BuildStarted, false)) return;
        EditorApplication.delayCall += Run;
    }

    static bool HasCharacter() =>
        AssetDatabase.IsValidFolder("Assets/Characters") &&
        AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Characters" }).Length > 0;

    internal static void Run()
    {
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) { EditorApplication.delayCall += Run; return; }
        if (File.Exists(Marker) || SessionState.GetBool(BuildStarted, false)) return;
        if (!HasCharacter())
        {
            if (SessionState.GetBool(Explained, false)) return;
            SessionState.SetBool(Explained, true);
            EditorUtility.DisplayDialog("VH Demo Project",
                "Two imports before the demo scenes can be built (Assets → Import Package → Custom Package…):\n\n" +
                "1. OVRLipSync (the Meta package you downloaded on Day 1)\n" +
                "2. Camila_Character.unitypackage (from the course email)\n\n" +
                "The scenes are built automatically after Camila is imported. Then open Assets/Scenes/00_Index and press Play.",
                "OK");
            return;
        }
        SessionState.SetBool(BuildStarted, true);
        MasterclassBuilder.BuildEverything();
    }
}

// Importing the character triggers the first build.
class MasterclassCharacterImport : AssetPostprocessor
{
    static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
    {
        if (File.Exists("Assets/Scenes/00_Index.unity")) return;
        if (imported.Any(p => p.StartsWith("Assets/Characters/") && p.EndsWith(".prefab")))
            EditorApplication.delayCall += MasterclassAutoBuild.Run;
    }
}
