using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

// Play (in 00_Index, a Day 2 or a webinar scene) starts the Python server by itself, in its own terminal window, if it is not already running:
// VH_Demo_Project/PythonProject/run_server.bat (Windows) or run_server.sh (macOS, Linux).
// The first time it also creates the .venv, installs the requirements and gets the speech models.
// Close the terminal window to stop the server. Menu: Masterclass → Python Server.
[InitializeOnLoad]
static class PythonServerLauncher
{
    const string PrefKey = "Masterclass.AutoStartServer";
    const string LastLaunch = "Masterclass.ServerLaunchedAt";
    const int Port = 8000;
    const string MenuAuto = "Masterclass/Python Server/Start on Play";

    static PythonServerLauncher()
    {
        EditorApplication.playModeStateChanged += s =>
        {
            if (s == PlayModeStateChange.ExitingEditMode && EditorPrefs.GetBool(PrefKey, true) && NeedsServer()) Launch(false);
        };
    }

    // Day 2 and webinar scenes talk to the server; the index too, since you can jump to them from it. Day 1 needs no server.
    static bool NeedsServer()
    {
        string scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        return scene == "00_Index" || scene.StartsWith("D2_") || scene.StartsWith("W_");
    }

    static string PythonDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "..", "PythonProject"));

    [MenuItem(MenuAuto, priority = 40)]
    static void ToggleAuto() => EditorPrefs.SetBool(PrefKey, !EditorPrefs.GetBool(PrefKey, true));
    [MenuItem(MenuAuto, true)]
    static bool ToggleAutoValidate() { Menu.SetChecked(MenuAuto, EditorPrefs.GetBool(PrefKey, true)); return true; }

    [MenuItem("Masterclass/Python Server/Start Now", priority = 41)]
    static void StartNow() => Launch(true);

    static bool ServerUp()
    {
        try
        {
            using (var c = new TcpClient())
                return c.ConnectAsync("127.0.0.1", Port).Wait(300) && c.Connected;
        }
        catch { return false; }
    }

    static void Launch(bool manual)
    {
        if (ServerUp()) { if (manual) Debug.Log($"Python server already running on port {Port}."); return; }

        // The server takes a few seconds to load Whisper and Piper (minutes on the very first run):
        // don't open a second window if one was opened recently.
        double last = SessionState.GetFloat(LastLaunch, -1000f);
        if (!manual && EditorApplication.timeSinceStartup - last < 120) return;

        string dir = PythonDir;
        try
        {
            ProcessStartInfo psi;
            if (Application.platform == RuntimePlatform.WindowsEditor)
            {
                // Shell-executing the .bat opens it in a new console window.
                string bat = Path.Combine(dir, "run_server.bat");
                if (!File.Exists(bat)) { Debug.LogWarning("Python server: " + bat + " not found."); return; }
                psi = new ProcessStartInfo(bat) { WorkingDirectory = dir, UseShellExecute = true };
            }
            else
            {
                string sh = Path.Combine(dir, "run_server.sh");
                if (!File.Exists(sh)) { Debug.LogWarning("Python server: " + sh + " not found."); return; }
                if (Application.platform == RuntimePlatform.OSXEditor)
                {
                    string inner = "bash '" + sh.Replace("'", "'\\''") + "'";                 // bash '/path/run_server.sh'
                    string script = "tell application \"Terminal\" to do script \"" + inner + "\"";
                    psi = new ProcessStartInfo("osascript",
                        "-e \"" + script.Replace("\"", "\\\"") + "\" -e \"tell application \\\"Terminal\\\" to activate\"")
                    { UseShellExecute = false };
                }
                else
                {
                    psi = new ProcessStartInfo("x-terminal-emulator", "-e bash \"" + sh + "\"") { UseShellExecute = false };
                }
            }
            Process.Start(psi);
            SessionState.SetFloat(LastLaunch, (float)EditorApplication.timeSinceStartup);
            Debug.Log("Python server: starting in its own window (" + dir + "). The panel shows 'online' when it is ready.");
        }
        catch (Exception e)
        {
            Debug.LogWarning("Python server: could not open a terminal (" + e.Message + "). Start it by hand: " + dir + "/run_server");
        }
    }
}
