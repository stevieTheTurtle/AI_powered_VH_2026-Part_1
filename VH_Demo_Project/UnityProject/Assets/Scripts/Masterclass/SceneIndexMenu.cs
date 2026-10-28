using UnityEngine;
using UnityEngine.SceneManagement;

// 00_Index scene: one button per stage scene. An entry without a scene is shown as a note (e.g. Hour 4, Python only).
public class SceneIndexMenu : MonoBehaviour
{
    [System.Serializable] public class Entry { public string scene; public string label; public string detail; }
    [SerializeField] Entry[] entries = new Entry[0];

    GUIStyle titleStyle, detailStyle, noteStyle, button;
    Vector2 scroll;

    void OnGUI()
    {
        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            detailStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            noteStyle = new GUIStyle(GUI.skin.box) { fontSize = 14, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(14, 14, 8, 8) };
            button = new GUIStyle(GUI.skin.button) { fontSize = 16, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(14, 14, 8, 8) };
        }
        float w = Mathf.Min(760, Screen.width - 40);
        GUILayout.BeginArea(new Rect((Screen.width - w) / 2, 20, w, Screen.height - 40));
        GUILayout.Label("AI-Powered Virtual Humans · Part 1 · Demo", titleStyle);
        GUILayout.Label("One scene per stage of Day 1, Day 2 and the webinar. Each scene contains everything built up to that point. "
                      + "Click a stage; in the scene, the panel top left has the buttons to try it and a button back here.", detailStyle);
        GUILayout.Space(10);
        scroll = GUILayout.BeginScrollView(scroll);
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.scene))
            {
                GUILayout.Label($"<b>{e.label}</b>\n{e.detail}", new GUIStyle(noteStyle) { richText = true });
            }
            else
            {
                if (GUILayout.Button(e.label, button, GUILayout.Height(38))) SceneManager.LoadScene(e.scene);
                GUILayout.Label(e.detail, detailStyle);
            }
            GUILayout.Space(6);
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}
