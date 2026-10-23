# VH Demo Project: every stage of Day 1 and Day 2, ready to try

A separate Unity project that shows the result of each stage of the course, one scene per stage, in the order of the slides.
You don't need to write code or know Unity: open it, import two packages, press Play and click a stage.

It contains the **solutions of Day 1 and Day 2**. Your own work stays in `VH_Starter_Project`: this project never touches it.

## What you need

* **Unity 6000.3.26f1** (the same as the course).
* For the Day 2 stages: **Python 3.12** and the **Ollama app**, running and signed in (see `Software_&_Downloads.md`).
* Two packages you already have from the course: **OVRLipSync** (Meta, Day 1) and **Camila_Character.unitypackage** (course email).

## Open it (once)

1. **Unity Hub → Add → Add project from disk** → pick `VH_Demo_Project/UnityProject`, open it with **6000.3.26f1**. The first import takes a few minutes.
2. **Assets → Import Package → Custom Package…** → the **OVRLipSync** package → Import.
   If the Console shows an error in `LipSyncDemo_SetCurrentTarget.cs`, delete that file (as on Day 1).
3. **Assets → Import Package → Custom Package…** → **Camila_Character.unitypackage** → Import.
   The demo scenes are **built automatically** right after (about a minute). If nothing happens: menu **Masterclass → Build Demo Scenes**.
4. Open **Assets/Scenes/00_Index**, press **Play**, click a stage.

In every scene the panel at the top left says what you are looking at and has the buttons to try it; **◀ All stages** goes back to the menu. **Tab** hides the panel.

## The stages

| # | Scene | Slides | What you see |
| :-- | :-- | :-- | :-- |
| 1 | Character on the stage | Day 1 · Hour 2 | Stage, camera framing head and chest, idle loop |
| 2 | Blink | Day 1 · Hour 2 · LIVE-1 | The eyelids close every 2-6 s |
| 3 | Gaze | Day 1 · Hour 2 | Head and eyes follow the camera (move it with the buttons), small saccades |
| 4 | Speaking layer | Day 1 · Hour 3 · LIVE-2 | Speaking on/off: arms and torso talk, legs stay in idle |
| 5 | Lip-sync | Day 1 · Hour 3 | A sample clip: the mouth follows the audio |
| 6 | Voice and body | Day 1 · Hour 3 · TURN-1 | The sample clip with lips **and** the talk loop |
| — | Hour 4 | Day 2 · Hour 4 | Python only, no scene: `python brain.py`, `python try_pipeline.py <recording>` in `PythonProject` |
| 7 | Typed chat | Day 2 · Hour 5 · TURN-3 | Type a message: the character answers with its voice |
| 8 | Microphone and upload | Day 2 · Hour 6 | Hold Space and speak: you hear your recording and read what Whisper heard |
| 9 | Voice loop | Day 2 · Hour 6 · LIVE-6, TURN-4 | Hold Space, speak, release: the character answers; timings in the panel |

## The Python server starts by itself

Press **Play** in `00_Index` or in a Day 2 scene and a terminal window opens running the server (`PythonProject/run_server.bat`, or `run_server.sh` on macOS). Wait until the panel says **Server: online**. Close that window to stop the server.

The very first time it prepares everything, which takes a few minutes:

* the Python environment: it reuses the `.venv` you made in `VH_Starter_Project/PythonProject`, or creates one here;
* the speech models: it copies them from `VH_Starter_Project/PythonProject/models`, or downloads them.

To turn this off: **Masterclass → Python Server → Start on Play**. To start it by hand: **Masterclass → Python Server → Start Now**, or double-click `PythonProject/run_server.bat`.

## Troubleshooting

| Problem | Fix |
| :-- | :-- |
| Capsule instead of Camila, or pink materials | Camila not imported yet: import it, then **Masterclass → Build Demo Scenes** |
| The mouth only opens and closes with the volume | OVRLipSync not imported: import it, then **Masterclass → Build Demo Scenes** |
| Server: starting or offline | Look at the server window: the first start installs things; an error there says what is missing |
| "Python was not found" in the server window | Install Python 3.12 with "Add python.exe to PATH", then press Play again |
| The character repeats your words | The Ollama app is not running or not signed in |
| Space does nothing | Click inside the Game view first; allow the microphone if the system asks |
