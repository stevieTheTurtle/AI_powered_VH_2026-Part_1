# VH Starter Project — AI powered Virtual Humans, Part 1

Student template: the state at the start of the course. Stripped copy of the instructor's reference solution (`VH_Final_Project`).

```
VH_Starter_Project/
├── UnityProject/                 Unity 6000.3.26f1, URP 17.3
│   └── Assets/
│       ├── Characters/CC4_Camila_DEMO/   course character (Reallusion CC4, 284 blendshapes, HDRP materials: pink in URP)
│       ├── Characters/Camila_URP.prefab  the character to use (URP materials in Characters/URP_Materials/)
│       ├── Animations/                   Mixamo/ (idle, wave, thinking) and VHToolkit/ (talk beats, shrug), with licences
│       ├── Settings/                     URP asset + renderer
│       ├── Scripts/                      every course script; most are complete, some have marked blocks
│       ├── Audio/                        visemes_debug.wav, transitions_debug.wav: lip-sync test clips (Hour 3)
│       └── Scenes/ Plugins/              empty: you fill them during the course
├── to_import/LINKS.md               every link you need: software, models, OVRLipSync
├── to_import/VisemeDriver.cs        copy into Assets/Scripts/ after importing OVRLipSync
├── to_import/VisemePreset.txt       viseme mapping: copy all, right-click Visemes → Paste
└── PythonProject/                   brain, ears (stt), voice (tts), server (main), contract (schemas), try_pipeline,
                                     requirements.txt, .env.example, models/ (empty), stages/bridge_mock.py
```

**How the code works in this course.** Every script is already here. Look for two kinds of marked blocks:

* `// ===== LIVE-n` (or `# ===== LIVE-n`): we write it together in class.
* `// ===== TURN-n`: you write it, from the hint in the comment.

Each block says which two parts of the system it connects. Until it is written, a placeholder keeps the project running (an echo, an empty method), so you can always press Play or start the server.

**Not included** (you get them yourself): OVRLipSync (Meta licence), the Whisper and Piper models (`PythonProject/models/README.md`), the Ollama app (install and sign in, see `Software_&_Downloads.md`).
