# AI-Powered Virtual Humans · Part 1: Virtual Humans Foundations

Politecnico di Torino · Stefano Calzolari · Wed 21 – Fri 23 October 2026

In this part of the masterclass we build an **Embodied Conversational Agent** step by step: a Unity character that looks at you, listens to your voice, thinks with a language model and answers with a voice, lip-sync, gestures and emotions.

| When | What |
|---|---|
| **Day 1** (Hours 1–3) | Theory of virtual humans; the body in Unity: character, idle and blinking, gaze, a talking layer, lip-sync |
| **Day 2** (Hours 4–6) | The brain in Python (an LLM through the Ollama app), ears (Whisper) and voice (Piper); the FastAPI bridge to Unity; the voice loop |
| **Webinar** (online) | Gestures and emotions chosen by the LLM through structured output |

## What is in this repository

| Path | Content |
|---|---|
| `Software_&_Downloads.md` | **Read this first**: everything to install and download before the course |
| `VH_Starter_Project/` | The course template: `UnityProject/` (Unity 6000.3.26f1), `PythonProject/` (brain, ears, voice, server), `to_import/` (files for Hour 3) |
| `Slides/` | The slides as PDF, published day by day |

## TL;DR: prepare before the course

About 30 min, mostly downloads, ~25 GB of free disk space. Details in [`Software_&_Downloads.md`](Software_&_Downloads.md).

1. Install **Unity Hub** and Unity **6000.3.26f1** (Personal licence).
2. Install **Python 3.12** (Windows: tick *Add to PATH*).
3. Install the **Ollama app**, sign in with a free account, check that `ollama run gpt-oss:120b-cloud "hi"` answers.
4. Download the speech models (**Whisper small**, **Piper English voice**) into `VH_Starter_Project/PythonProject/models/`.
5. Download **OVRLipSync 29.0.0** from Meta (accept the licence). No need to import it yet; please keep it out of Git (Meta's licence).
6. Download this repository to a short path without spaces, open `VH_Starter_Project/UnityProject` once in Unity **at home** (the first import takes 10+ minutes), then import **`Camila_Character.unitypackage`** from the link in the course email (*Assets → Import Package → Custom Package…*).
7. A code editor (Rider and VS Code suggested), and a microphone that works.
8. *Optional, recommended:* **Git**, to save a checkpoint of your project at the end of each hour.

Something not working? Feel free to write to the instructor before the course, with a screenshot, your operating system and the Unity version: we will sort it out together.

## Licences

- **Code** (Unity C# scripts, Python project): MIT.
- **Slides and documents**: CC BY-NC 4.0.
- **Third-party content** (Mixamo and USC VHToolkit animations, Unity packages) keeps its own licence, noted next to the files.
- The character Camila is **not** in this repository: it is shared with course participants as a Unity package, licensed for this course only (see `CHARACTER_LICENSE.md` inside the package). Please do not commit it to any public repository.

Full details in [`LICENSE.md`](LICENSE.md).
