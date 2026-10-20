# AI-Powered Virtual Humans — Part 1: software and downloads

About 30 min, mostly downloads. At the end, the checklist in §8 helps you confirm that everything works. If something doesn't, feel free to write to the instructor before the course: it is much easier to sort it out in advance.

Everything here is free. You need a stable connection and about **25 GB** of free disk space.

---

## TL;DR

About 30 min (mostly downloads), ~25 GB free:

1. **Unity Hub** + Unity **6000.3.26f1**, Personal licence (§2).
2. **Python 3.12**, added to PATH (§4).
3. **Ollama app**, signed in with a free account; `ollama run gpt-oss:120b-cloud "hi"` should answer (§5).
4. **Speech models**: Whisper *small* + the Piper English voice, into `PythonProject/models/` (§5.1).
5. **OVRLipSync 29.0.0** ZIP from Meta: just download it, we import it together in class (§6).
6. **Project template**: unzip to a short path, open `UnityProject/` once in Unity at home (first import takes 10+ min), then import the character package **Camila_Character.unitypackage** from the link in the course email (§7).
7. A code editor (Rider and VS Code suggested, any is fine) and a **microphone** that works (§3, §5.2).
8. *Optional, recommended:* **Git**, to save a checkpoint of your project at the end of each hour (§3).

Then go through the checklist in §8. Any problem? Feel free to write to the instructor before the course.

---

## 1. What you install, at a glance

| # | Software | Who provides it |
|---|---|---|
| 1 | Unity Hub + Unity **6000.3.26f1** (Unity 6.3) | You, from Unity |
| 2 | Git (**optional, recommended**) | You |
| 3 | Code editors, **suggested**: **JetBrains Rider** (C#, Unity) and **VS Code** (Python); any IDE you prefer is fine | You |
| 4 | Python **3.12** | You |
| 5 | **Ollama app**, signed in with a free account | You |
| 6 | **OVRLipSync** (Meta) | You, individually (Meta asks each user to accept its licence) |
| 7 | **Project template** (character, animations, scripts, Python starter) | The instructor |
| 8 | A working **microphone** and a recorder app (Windows **Sound Recorder**, macOS **QuickTime**) | Already on your computer |

Python packages are installed later by one command from the template. The speech models (§5.1) are too big for Git, so you download them yourself.

---

## 2. Unity (required)

1. Install **Unity Hub**: <https://unity.com/download>
2. Sign in with a free Unity account and activate a **Personal** licence (Hub → Manage licences).
3. In the Hub: **Installs → Install Editor → Archive**, or open the version archive at <https://unity.com/releases/editor/archive>, and install exactly **6000.3.26f1**.
   - A different 6000.3.x patch usually works, but a different major version does not. Unity 2022.3 will not work.
   - If the Hub offers modules, you don't need any (no Android or iOS).
4. Wait for the install to finish and confirm the version shows in **Installs**.

## 3. Code editors and Git

The two editors below are **suggested, not mandatory**: they are what the instructor uses on screen. If you already work with another IDE (Visual Studio, VS Code for C#, PyCharm, …), keep it.


- **Git** (*optional, recommended*): <https://git-scm.com/downloads>. Check with `git --version`. The course saves a checkpoint (a commit with a tag) at the end of each hour, so you can always see your progress and go back to a working state; without Git, simply copy the project folder at each checkpoint.
- **JetBrains Rider** for the C# side (Unity scripts): <https://www.jetbrains.com/rider/> (free for students and non-commercial use). In Unity: *Edit → Preferences → External Tools → External Script Editor = Rider*.
- **VS Code** for the Python side (brain, ears, voice, server): <https://code.visualstudio.com/>. Install the **Python** extension (Microsoft) and open the `PythonProject/` folder; pick the `.venv` interpreter when it asks (Hour 4).
- If you use Git, set your identity once:

```bash
git config --global user.name  "Your Name"
git config --global user.email "you@example.com"
```

## 4. Python

- Install **Python 3.12** (the course was built and tested with 3.12.x; other versions may fail to install the speech packages) from <https://www.python.org/downloads/>.
  - Windows: tick **Add python.exe to PATH** in the installer.
- Check in a terminal: `python --version` (on macOS/Linux, `python3 --version`).
- Nothing else to install yet: the virtual environment and packages are created in Hour 4 from the template's `requirements.txt`.

## 5. Ollama app (the brain)

The brain runs through the **Ollama app** on your computer. Signed in with a free ollama.com account, the app also serves **cloud models**: they run on Ollama's servers on the free quota, so you do **not** need a strong GPU, a big download or an API key.

1. Install Ollama: <https://ollama.com/download> (Windows, macOS, Linux).
2. Create a free account at <https://ollama.com/> and sign in to it from the app (or in a terminal: `ollama signin`).
3. Test the course model in a terminal: `ollama run gpt-oss:120b-cloud "Say hello in one sentence"`. If it answers, you are ready.
4. Keep the app running during the course: the Python code talks to it at `http://localhost:11434`.

The free quota is enough for the course. If you hit a limit, tell the instructor.

## 5.1 Speech models (Whisper and Piper)

These run on your own computer (no account, no key). Download them now into a temporary folder; you move them into the template's `PythonProject/models/` folder after §7. Please keep the folder names exactly as written.

| Model | Link | Files to download | Folder name | Size |
|---|---|---|---|---|
| Whisper **small** (speech to text) | <https://huggingface.co/Systran/faster-whisper-small> → *Files* | `config.json`, `model.bin`, `tokenizer.json`, `vocabulary.txt` | `faster-whisper-small` | ≈ 0.5 GB |
| Piper English voice (default) | <https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US/hfc_female/medium> | `en_US-hfc_female-medium.onnx` **and** `en_US-hfc_female-medium.onnx.json` | `piper-voice-en_US-hfc_female-medium` | ≈ 63 MB |
| Piper Italian voice (optional) | <https://huggingface.co/rhasspy/piper-voices/tree/main/it/it_IT/serena/high> | `it_IT-serena-high.onnx` **and** `.onnx.json` | `piper-voice-it_IT-serena-high` | ≈ 114 MB |

- Each `.onnx` and its `.onnx.json` must stay in the same folder.
- You can listen to the voices first at <https://rhasspy.github.io/piper-samples/>.
- The final layout is: `PythonProject/models/faster-whisper-small/…`, `PythonProject/models/piper-voice-en_US-hfc_female-medium/…`.

## 5.2 Microphone

In Hour 4 you record a short spoken question and feed it to the speech recogniser; in Hour 6 you talk to the character live.

- **Windows:** search for **Sound Recorder** in the Start menu, record a few seconds and play it back. Recordings are saved as `.m4a` in `Documents\Sound Recordings`: the course code reads that format directly.
- **macOS:** QuickTime Player → File → New Audio Recording.
- Allow microphone access when the system asks (also for Unity, the first time you press Play in Hour 6).

## 6. OVRLipSync (Meta): you download it yourself

Meta's licence does not allow the instructor to hand it out, so the template does **not** contain it. Each student downloads it and accepts the terms.

1. Open Meta's OVRLipSync page: <https://developers.meta.com/horizon/documentation/unity/audio-ovrlipsync-unity/> (the downloads page is <https://developers.meta.com/horizon/downloads/package/oculus-lipsync-unity/>).
2. Read and accept the licence, then download the **Unity package**. The course uses **OVRLipSync 29.0.0**: the file is a ZIP of about 110 MB (`ovr_lipsync_unity_29.0.0.zip`) that contains `OculusLipSync.unitypackage`.
3. Keep the ZIP in `Downloads/`. Please keep it for yourself: Meta's licence does not allow committing it to Git or sharing it.
4. You import it into the project in Hour 3; the instructor shows the steps. Until then the Console may show errors about missing `OVRLipSync` types. That is expected.

If Meta asks for a developer account, create one (free).

## 7. The project template (from the instructor)

The template is one folder with two parts:

- **`UnityProject/`**: Unity 6000.3.26f1 project with the animation clips (`Assets/Animations/Mixamo/` and `Assets/Animations/VHToolkit/`), two lip-sync test clips (`Assets/Audio/`), URP settings and every course script.
- **`PythonProject/`**: the Python side (`brain.py`, `stt.py`, `tts.py`, `main.py`, …), `requirements.txt`, `.env.example` and an empty `models/` folder.
- **`to_import/`**: files you copy into Unity in Hour 3, after importing OVRLipSync (`VisemeDriver.cs`, `VisemePreset.txt`), and `LINKS.md` with every link in this sheet.

**Not inside the template:** OVRLipSync (§6), and the speech models (§5.1); the Ollama app is installed separately (§5).

**Where:** the `VH_Starter_Project/` folder of this repository.

1. Get this repository: green **Code** button → **Download ZIP**, or `git clone` it.
2. Put it somewhere with a **short path without spaces or accents**, for example `C:\VH\` or `~/VH/`. From here on, "the template" is its `VH_Starter_Project/` folder.
3. Move the model folders from §5.1 (two, or three with the Italian voice) into `PythonProject/models/`.
4. Open **Unity Hub → Add → Add project from disk** and pick the template's `UnityProject` folder.
5. Open it with **6000.3.26f1**. The **first import takes several minutes** (10+ on a slow disk): do this at home, not in class.
6. **Import the character.** The character Camila is not in the repository: download **`Camila_Character.unitypackage`** (about 250 MB) from the link in the course email. In Unity: **Assets → Import Package → Custom Package…** → pick the file → **Import** (everything ticked). It lands in `Assets/Characters/`. Please read `Assets/Characters/CHARACTER_LICENSE.md`: the character is for this course only, so please don't share it or commit it to a public repository.
7. When the import finishes, select `Assets/Characters/Camila_URP` in the Project window: the preview shows the character with normal colours, not pink. (`Assets/Scenes/` is empty on purpose: you create the scene in Hour 2.)
8. Git (optional, recommended): if you cloned the template, run `git status` once to confirm it works. If you downloaded the ZIP, turn the folder into a repository once, so you can commit a checkpoint at the end of each hour:

```bash
git init
git add -A
git commit -m "start"
```

Expected state at this point: the character looks correct, not pink, and the Console is clean **except** for OVRLipSync-related errors (see §6).

## 8. Final checklist (do it the day before)

- [ ] *(optional)* `git --version` prints a version
- [ ] `python --version` prints 3.12.x
- [ ] Unity Hub shows Editor **6000.3.26f1** and a Personal licence
- [ ] The template opens in Unity and the first import is finished
- [ ] The Game view and the character are not pink
- [ ] The OVRLipSync ZIP is downloaded and saved (not imported yet)
- [ ] The Whisper and Piper model folders are downloaded (§5.1) and sit in `PythonProject/models/`
- [ ] The Ollama app is installed and signed in, and `ollama run gpt-oss:120b-cloud "hi"` answers
- [ ] A short Sound Recorder (or QuickTime) recording plays back with your voice
- [ ] 10 GB or more of free disk space left

## 9. If something goes wrong

| Problem | What to do |
|---|---|
| The project says it was created with a different version | Install exactly 6000.3.26f1 from the archive and reopen. |
| Import takes more than 10 minutes | Antivirus or a slow disk: close other programs and wait; ask the instructor for the pre-imported Library zip if it keeps failing. |
| Pink character or materials | Use `Assets/Characters/Camila_URP.prefab`, not the `CC4_Camila_DEMO` original (HDRP materials). Still pink? Let the instructor know, and please don't convert the materials yourself. |
| Console full of errors after import | Check that OVRLipSync is the only cause; send a screenshot of the first error. |
| `python` is not found | Reinstall with *Add to PATH*, or use `py` (Windows) / `python3` (macOS, Linux). |
| Meta page will not let you download | Sign in with a Meta developer account and accept the licence again. |

If anything goes wrong, feel free to write to the instructor before the course, with a screenshot, your operating system and the Unity version: we will sort it out together.
