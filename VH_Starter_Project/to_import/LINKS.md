# AI-Powered Virtual Humans: links to everything you need

Everything is free. Full instructions (what to click, where to put each file) are in
`Software_&_Downloads.md` (in the repository root). Do the downloads **before the course**.

## 1. Software to install

| What | Version | Link | Used in |
|---|---|---|---|
| Unity Hub | latest | <https://unity.com/download> | all Unity hours |
| Unity Editor | **6000.3.26f1** exactly | Hub → Installs → Install Editor → Archive, or <https://unity.com/releases/editor/archive> | Hours 2, 3, 5, 6, webinar |
| Git | latest | <https://git-scm.com/downloads> | every checkpoint |
| JetBrains Rider (C# editor, suggested) | latest | <https://www.jetbrains.com/rider/> (free for students and non-commercial use) | all Unity / C# work |
| VS Code (Python editor, suggested) | latest | <https://code.visualstudio.com/> + the **Python** extension | all Python work: Hours 4, 5, 6, webinar |
| Python | **3.12.x** | <https://www.python.org/downloads/> (Windows: tick *Add python.exe to PATH*) | Hours 4, 5, 6, webinar |
| Ollama app | latest | <https://ollama.com/download>, then a free account at <https://ollama.com/> and sign in (`ollama signin`) | the brain: Hours 4, 5, 6, webinar |

Rider and VS Code are only suggestions (they are what you see on screen): any IDE you already use is fine.

Test the brain: `ollama run gpt-oss:120b-cloud "Say hello in one sentence"`.

## 2. Files to download (they are not in the template)

| What | Link | Files | Put them in |
|---|---|---|---|
| Whisper **small** (speech to text) | <https://huggingface.co/Systran/faster-whisper-small> → *Files* | `config.json`, `model.bin`, `tokenizer.json`, `vocabulary.txt` | `PythonProject/models/faster-whisper-small/` |
| Piper English voice | <https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US/hfc_female/medium> | `en_US-hfc_female-medium.onnx` + `.onnx.json` | `PythonProject/models/piper-voice-en_US-hfc_female-medium/` |
| Piper Italian voice (optional) | <https://huggingface.co/rhasspy/piper-voices/tree/main/it/it_IT/serena/high> | `it_IT-serena-high.onnx` + `.onnx.json` | `PythonProject/models/piper-voice-it_IT-serena-high/` |
| OVRLipSync **29.0.0** (Meta, accept the licence yourself) | <https://developers.meta.com/horizon/downloads/package/oculus-lipsync-unity/> (docs: <https://developers.meta.com/horizon/documentation/unity/audio-ovrlipsync-unity/>) | `ovr_lipsync_unity_29.0.0.zip` (≈ 110 MB) | keep the ZIP in `Downloads/`, import it in Hour 3; do not commit it |

Listen to the Piper voices first: <https://rhasspy.github.io/piper-samples/>

## 3. Already in the template (no download, for reference)

| What | Source | Licence |
|---|---|---|
| Camila, the course character (Character Creator 4) | Reallusion CC4 demo character | Reallusion terms |
| Idle, Waving, Thinking clips | Mixamo (Adobe): <https://www.mixamo.com/> | Mixamo terms (free account) |
| Talk beats and Shrug clips | USC ICT Virtual Human Toolkit: <https://github.com/USC-ICT/vhtoolkit> | USC-RL v3.0, educational / non-commercial; keep `LICENSE_USC-RL.md` |
| Animation Rigging (gaze) | Unity package `com.unity.animation.rigging` 1.4.1, installed automatically when the project opens | Unity |

## 4. In this folder

- `VisemeDriver.cs`: copy it into `UnityProject/Assets/Scripts/` **after** importing OVRLipSync (Hour 3, §3.4b of the guide).
- `VisemePreset.txt`: the 15-viseme mapping for Camila. Open it, **Ctrl+A, Ctrl+C**, then in Unity right-click **Visemes** on VisemeDriver → **Paste**. Do not edit the file.

## 5. During the course

- Example of idle life (blinks and saccades), Hour 2: <https://tore-knabe.com/WebGL/REM3/index.html>
