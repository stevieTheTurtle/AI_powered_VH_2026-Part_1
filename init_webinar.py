"""Bring your PythonProject to the webinar starting point ("From words to meaningful gestures").

Run it from the repository root (the folder with download_models.py and VH_Starter_Project):

    python init_webinar.py                  # updates VH_Starter_Project/PythonProject
    python init_webinar.py <folder>         # or another project / PythonProject folder
    python init_webinar.py --dry-run        # only show what would change

After it, your Python side is exactly where the webinar starts:
  * Day 1-2 gaps solved: LIVE-3 (think_text), TURN-2 (try_pipeline), LIVE-4 (respond), TURN-4 (/voice-chat)
  * webinar gaps still open: TURN-5 (schemas.py), TURN-6 and LIVE-8 (brain.py); think() is still text only
  * run_server.bat / run_server.sh, which Unity's Masterclass -> Python Server menu starts

Your files are never lost: every file that changes is first copied to
PythonProject/_before_webinar_<date>/. Your .env, .venv, speech models and audio are not touched.
Only the Python standard library is used, so it runs with any Python 3.
"""
import argparse
import py_compile
import shutil
import sys
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent

# relative path -> content at the webinar starting point
FILES = {
    # ------------------------------------------------------------ brain.py
    "brain.py": r'''# brain.py — Brain: THE SEAM. think(text) -> BrainOutput
#
#   think_text()        one LLM call, text in, text out
#   think_structured()  the same call, returning JSON that also chooses a gesture and an emotion
#   No Ollama app:      echo (it repeats what you said), so nothing breaks in front of the class
#   Part 2:             replace the body of think() with an agent. Nothing else changes.
import os, re
from dotenv import load_dotenv
from pydantic import ValidationError
from schemas import BrainOutput

load_dotenv()                                            # reads PythonProject/.env (also when run alone)


# ---------------------------------------------------------------- text only
TEXT_PROMPT = """You are Camila, a friendly virtual assistant who lives
inside a 3D scene and talks with a person face to face.
Answer in one or two short sentences, in the language of the user.
Warm, natural and spoken; no lists, no markdown, no emojis."""


# ---------------------------------------------------------------- structured (body language)
SYSTEM_PROMPT = """You are Camila, a friendly virtual assistant who lives inside a 3D scene and talks with a person face to face.

STYLE
- Answer in one or two short sentences, in the language of the user.
- Warm, natural and spoken; no lists, no markdown, no emojis.

OUTPUT
Reply with ONE JSON object and nothing else:
{"speech_text": "<what you say>", "action": "<ACTION>", "emotion": "<EMOTION>"}

ACTION must be exactly one of: NONE, WAVE, SHRUG, THINKING
<TURN-6: one line per action saying WHEN to use it, e.g. "- WAVE: greetings and goodbyes">

EMOTION must be exactly one of: NEUTRAL, HAPPY, SAD
Choose the emotion that fits your reply. Bad news or sad topics: SAD, usually with NONE.
Never use an action or emotion that is not in the lists."""

# The Ollama app runs locally (http://localhost:11434). Signed in with a free ollama.com
# account, it also serves cloud models (names ending in -cloud): they run on Ollama's servers
# on the free quota, so no GPU and no key in .env. A model you pulled runs on your machine.
# Without the app: LLM_HOST=https://ollama.com plus OLLAMA_API_KEY calls the cloud directly.
# (Not OLLAMA_HOST: Ollama installs often set that machine-wide for the local server.)
_KEY = os.getenv("OLLAMA_API_KEY")
_HOST = os.getenv("LLM_HOST", "http://localhost:11434")
_LOCAL = "localhost" in _HOST or "127.0.0.1" in _HOST
_MODEL = os.getenv("LLM_MODEL", "gpt-oss:120b-cloud")
_TEMPERATURE = float(os.getenv("LLM_TEMPERATURE", "0.3"))  # low = less inventive

_client = None
if _KEY or _LOCAL:
    from ollama import Client
    _client = Client(host=_HOST,
                     headers={"Authorization": "Bearer " + _KEY} if _KEY else None)
    if _LOCAL:
        try:
            _client.list()                                # is the Ollama app running?
        except Exception:
            print("Brain: Ollama app not reachable at", _HOST, "-> echo mode")
            _client = None


def brain_name() -> str:
    return "echo" if _client is None else "llm"


def think_text(user_text: str) -> str:
    """Text in, what to say out."""
    if _client is None:
        return user_text                                 # echo: Ollama app not running
    # LIVE-3 (solution)
    try:
        resp = _client.chat(model=_MODEL, messages=[
            {"role": "system", "content": TEXT_PROMPT},
            {"role": "user", "content": user_text}])
        return resp.message.content.strip()
    except Exception as e:                               # network, auth, quota used up...
        print("Brain: LLM call failed:", e)
        return user_text


def _parse(raw: str) -> BrainOutput:
    """The model's JSON -> a validated BrainOutput (raises if it does not fit the contract)."""
    raw = raw.strip()
    raw = re.sub(r"^```(?:json)?\s*|\s*```$", "", raw)   # strip a code fence if present
    return BrainOutput.model_validate_json(raw)


def _fallback() -> BrainOutput:
    """What the body does when the brain fails: a shrug, never a crash."""
    return BrainOutput(speech_text="Sorry, I lost my train of thought. Could you say that again?",
                       action="SHRUG", emotion="NEUTRAL")


def think_structured(user_text: str) -> BrainOutput:
    """The same single call, now returning speech + action + emotion."""
    if _client is None:
        return BrainOutput(speech_text=user_text)        # echo
    # ===== LIVE-8 · Brain ▸ LLM ▸ Contract · structured output =====
    # Connects the model to the contract: the same _client.chat call, with SYSTEM_PROMPT,
    # format=BrainOutput.model_json_schema() and options={"temperature": _TEMPERATURE};
    # then _parse(reply). Invalid JSON (ValidationError, ValueError) or any other failure
    # -> print it and return _fallback().
    return BrainOutput(speech_text=think_text(user_text))   # placeholder: text only
    # ===== end LIVE-8 =====


def think(user_text: str) -> BrainOutput:
    """THE SEAM: the server only ever calls this."""
    # Text only for now. Body language: return think_structured(user_text) instead.
    return BrainOutput(speech_text=think_text(user_text))


if __name__ == "__main__":                               # python brain.py
    print("Brain:", brain_name())
    while True:
        print("Camila:", think(input("You: ")))
''',
    # ------------------------------------------------------------ main.py
    "main.py": r'''# main.py — Server: the bridge between the brain side (Python) and the body (Unity)
#   uvicorn main:app --reload --port 8000      ->  http://127.0.0.1:8000/docs
from dotenv import load_dotenv
load_dotenv()                                   # before importing brain/stt/tts: they read env vars

import shutil, tempfile, time
from contextlib import asynccontextmanager
from pathlib import Path
from typing import Optional

from fastapi import FastAPI, File, UploadFile
from fastapi.staticfiles import StaticFiles
from starlette.concurrency import run_in_threadpool

from schemas import BrainOutput, ChatRequest, ChatResponse
from stt import transcribe
from brain import think, brain_name
from tts import synthesize, cleanup, AUDIO_DIR

Path("static/audio").mkdir(parents=True, exist_ok=True)


@asynccontextmanager
async def lifespan(app: FastAPI):
    cleanup(max_age_s=0)
    if not (AUDIO_DIR / "response_01.wav").exists():   # the clip the bridge mock plays
        try:
            await synthesize("Hello! I am ready to assist you.", name="response_01")
        except Exception as e:
            print("Could not create response_01.wav:", e)
    print(f"Brain: {brain_name()}")
    yield


app = FastAPI(title="Virtual Human Server", lifespan=lifespan)
app.mount("/static", StaticFiles(directory="static"), name="static")   # Unity downloads the WAVs here


@app.get("/")
def root():
    return {"status": "ok", "brain": brain_name()}


def save_upload(audio: UploadFile) -> str:
    """Writes an uploaded file to a temporary .wav and returns its path (delete it when done)."""
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tmp:
        shutil.copyfileobj(audio.file, tmp)
    return tmp.name


async def respond(text: str, transcript: Optional[str] = None, t_stt_ms: int = 0) -> ChatResponse:
    """Text in -> what to say + a WAV to say it with. Shared by /chat and /voice-chat."""
    cleanup()
    t0 = time.perf_counter()
    # LIVE-4 (solution)
    out = await run_in_threadpool(think, text)
    t1 = time.perf_counter()
    try:
        audio_url = await synthesize(out.speech_text)
    except Exception as e:                      # missing audio -> the body still reacts
        print("TTS failed:", e)
        audio_url = None
    t2 = time.perf_counter()
    return ChatResponse(
        **out.model_dump(), audio_url=audio_url, transcript=transcript, brain=brain_name(),
        timings_ms={"stt": t_stt_ms, "brain": int((t1 - t0) * 1000), "tts": int((t2 - t1) * 1000)})


@app.post("/chat", response_model=ChatResponse)
async def chat(req: ChatRequest):
    return await respond(req.text)


@app.post("/voice-chat", response_model=ChatResponse)
async def voice_chat(audio: UploadFile = File(...)):
    # TURN-4 (solution)
    path = save_upload(audio)
    t0 = time.perf_counter()
    transcript = await run_in_threadpool(transcribe, path)
    t_stt = int((time.perf_counter() - t0) * 1000)
    Path(path).unlink(missing_ok=True)
    print(f"Heard: {transcript!r}")
    if not transcript:
        return await respond("Sorry, I did not catch that. Could you repeat?", "", t_stt)
    return await respond(transcript, transcript, t_stt)


@app.post("/stt-test")
def stt_test(audio: UploadFile = File(...)):
    """Stage test: upload a WAV in Swagger, read the transcript."""
    path = save_upload(audio)
    text = transcribe(path)
    Path(path).unlink(missing_ok=True)
    print("Transcript:", text)
    return {"transcript": text}
''',
    # ------------------------------------------------------------ schemas.py
    "schemas.py": r'''# schemas.py — the JSON contract between brain and body
from typing import Dict, Literal, Optional
from pydantic import BaseModel, ConfigDict

# The body's vocabulary: what the brain is allowed to ask for.
Action = Literal["NONE", "WAVE", "SHRUG", "THINKING"]
Emotion = Literal["NEUTRAL", "HAPPY", "SAD"]


class ChatRequest(BaseModel):
    text: str


class BrainOutput(BaseModel):
    # extra="forbid" -> "additionalProperties": false in the JSON schema sent to the LLM
    model_config = ConfigDict(extra="forbid")

    speech_text: str
    # ===== TURN-5 · Contract ▸ body language =====
    # Add two fields with safe defaults: `action` (an Action, default "NONE")
    # and `emotion` (an Emotion, default "NEUTRAL"). Unity already reads both names.
    # ===== end TURN-5 =====


class ChatResponse(BrainOutput):
    model_config = ConfigDict(extra="ignore")

    audio_url: Optional[str] = None
    transcript: Optional[str] = None
    timings_ms: Optional[Dict[str, int]] = None
    brain: Optional[str] = None      # "echo" or "llm": shown in the console
''',
    # ------------------------------------------------------------ try_pipeline.py
    "try_pipeline.py": r'''# try_pipeline.py — the whole brain side, no Unity, no server.
#   python try_pipeline.py Recording.m4a   (record it with Windows Sound Recorder; WAV works too)
import asyncio, sys, time
from stt import transcribe
from brain import think_text
from tts import synthesize

# TURN-2 (solution)
t0 = time.perf_counter(); heard = transcribe(sys.argv[1])
t1 = time.perf_counter(); reply = think_text(heard)
t2 = time.perf_counter(); audio = asyncio.run(synthesize(reply))
t3 = time.perf_counter()

print("Heard:", heard)
print("Reply:", reply)
print("Audio:", audio)
print(f"stt {t1-t0:.2f} s · brain {t2-t1:.2f} s · tts {t3-t2:.2f} s")
''',
    # ------------------------------------------------------------ stt.py
    "stt.py": r'''# stt.py — Ears: speech to text with faster-whisper (local model)
import os, sys
from pathlib import Path
import ctranslate2
from faster_whisper import WhisperModel

# Local model folder only: no download from Hugging Face.
# Default: PythonProject/models/faster-whisper-small. Override with WHISPER_MODEL=<folder>.
# Download the model files from https://huggingface.co/Systran/faster-whisper-small (see models/README.md).
_MODEL_PATH = Path(os.getenv("WHISPER_MODEL") or Path(__file__).parent / "models" / "faster-whisper-small")
_LANGUAGE = os.getenv("WHISPER_LANGUAGE") or None        # e.g. "en" or "it"; None = auto-detect
# GPU when an NVIDIA GPU is available, otherwise CPU. Valid devices: "cuda" or "cpu" (not "gpu").
_DEVICE = os.getenv("WHISPER_DEVICE") or ("cuda" if ctranslate2.get_cuda_device_count() > 0 else "cpu")

if not (_MODEL_PATH / "model.bin").exists():
    raise FileNotFoundError(
        f"Whisper model not found at {_MODEL_PATH}. "
        "Copy the faster-whisper model folder there, or set WHISPER_MODEL to its path.")


def _add_nvidia_dlls() -> None:
    """Windows: the NVIDIA pip wheels (requirements-gpu.txt) keep their DLLs in site-packages/nvidia/*/bin."""
    if os.name != "nt":
        return
    import glob, site
    for sp in site.getsitepackages():
        for d in glob.glob(os.path.join(sp, "nvidia", "*", "bin")):
            os.add_dll_directory(d)
            os.environ["PATH"] = d + os.pathsep + os.environ.get("PATH", "")


def _load() -> WhisperModel:
    if _DEVICE == "cuda":
        try:
            m = WhisperModel(str(_MODEL_PATH), device="cuda", compute_type="int8", local_files_only=True)
            # A missing CUDA library (e.g. cublas64_12.dll) only fails at the first inference: probe it now.
            import numpy as np
            list(m.transcribe(np.zeros(16000, dtype=np.float32))[0])
            print("Whisper: running on the GPU")
            return m
        except Exception as e:
            print(f"Whisper: GPU not usable ({e}); falling back to the CPU")
    return WhisperModel(str(_MODEL_PATH), device="cpu", compute_type="int8", local_files_only=True)


# Loaded once at startup, not per request.
_add_nvidia_dlls()
_model = _load()


def transcribe(path: str) -> str:
    segments, _info = _model.transcribe(path, vad_filter=True, language=_LANGUAGE)
    return " ".join(s.text.strip() for s in segments).strip()


if __name__ == "__main__":                       # python stt.py Recording.m4a (Windows Sound Recorder; WAV works too)
    print(transcribe(sys.argv[1]))
''',
    # ------------------------------------------------------------ tts.py
    "tts.py": r'''# tts.py — Voice: local text to speech with Piper, WAV for Unity
import asyncio, os, sys, threading, time, uuid, wave
from pathlib import Path

from piper import PiperVoice
from starlette.concurrency import run_in_threadpool

AUDIO_DIR = Path("static/audio")
AUDIO_DIR.mkdir(parents=True, exist_ok=True)
KEEP = {"response_01.wav"}                             # the bridge mock clip is never cleaned

# Local voices only: no network. TTS_LANGUAGE (en or it, default en) picks the voice, which lives in
# PythonProject/models/. TTS_MODEL=<path to a .onnx file> overrides it (its .onnx.json must sit next to it).
# Download: en https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US/hfc_female/medium
#           it https://huggingface.co/rhasspy/piper-voices/tree/main/it/it_IT/serena/high  (see models/README.md)
VOICES = {
    "en": "piper-voice-en_US-hfc_female-medium/en_US-hfc_female-medium.onnx",
    "it": "piper-voice-it_IT-serena-high/it_IT-serena-high.onnx",
}
LANGUAGE = (os.getenv("TTS_LANGUAGE") or "en").strip().lower()
if LANGUAGE not in VOICES and not os.getenv("TTS_MODEL"):
    raise ValueError(f"TTS_LANGUAGE must be one of {sorted(VOICES)}, got {LANGUAGE!r}")
_VOICE_PATH = Path(os.getenv("TTS_MODEL") or Path(__file__).parent / "models" / VOICES[LANGUAGE])

if not _VOICE_PATH.exists():
    raise FileNotFoundError(
        f"Piper voice not found at {_VOICE_PATH}. "
        "Copy the piper-voice folder there, or set TTS_MODEL to the .onnx file path.")

# Loaded once at startup, not per request.
_voice = PiperVoice.load(str(_VOICE_PATH))
_lock = threading.Lock()                               # one synthesis at a time


def cleanup(max_age_s: int = 600) -> None:
    """Housekeeping: delete generated audio older than a few minutes."""
    now = time.time()
    for f in AUDIO_DIR.glob("*.*"):
        if f.name not in KEEP and now - f.stat().st_mtime > max_age_s:
            f.unlink(missing_ok=True)


def _synth(text: str, wav_path: Path) -> None:
    with _lock, wave.open(str(wav_path), "wb") as f:   # 16-bit mono at the voice's rate (22050 Hz)
        _voice.synthesize_wav(text, f)


async def synthesize(text: str, name: str | None = None) -> str:
    """Returns a relative URL like /static/audio/<id>.wav"""
    wav = AUDIO_DIR / f"{name or uuid.uuid4().hex}.wav"
    await run_in_threadpool(_synth, text, wav)
    return f"/static/audio/{wav.name}"


if __name__ == "__main__":                      # python tts.py "Hello there"
    print(asyncio.run(synthesize(sys.argv[1])))
''',
    # ------------------------------------------------------------ requirements.txt
    "requirements.txt": r'''fastapi
uvicorn[standard]
python-multipart
pydantic>=2
faster-whisper
av==13.1.0
piper-tts==1.8.0
python-dotenv
ollama
# Whisper on an NVIDIA GPU (cublas64_12.dll, cudnn64_9.dll). Windows only; without a GPU Whisper runs on the CPU.
nvidia-cublas-cu12==12.9.2.10; sys_platform == "win32"
nvidia-cudnn-cu12==9.27.0.42; sys_platform == "win32"
''',
    # ------------------------------------------------------------ .env.example
    ".env.example": r'''# Copy to .env. The brain talks to the Ollama app on this machine (signed in to ollama.com).
# If the app is not running, the brain echoes (it repeats what you said).
# LLM_MODEL=gpt-oss:120b-cloud  # any "-cloud" model on ollama.com, or a model you pulled locally
# LLM_TEMPERATURE=0.3
# LLM_HOST=http://localhost:11434  # the Ollama app; https://ollama.com + OLLAMA_API_KEY= to skip the app
# WHISPER_MODEL=        # folder of the faster-whisper model; default models/faster-whisper-small (no download)
# WHISPER_LANGUAGE=     # empty = auto-detect, or en / it
# TTS_MODEL=   # LEAVE COMMENTED with Piper: TTS_LANGUAGE already picks the voice in models/. Only for another voice: full path of its .onnx FILE (never the folder), with <name>.onnx.json next to it
# TTS_LANGUAGE=en      # voice language: en or it (picks the Piper voice in models/). Italian also needs WHISPER_LANGUAGE=it
''',
    # ------------------------------------------------------------ stages/bridge_mock.py
    "stages/bridge_mock.py": r'''# Fallback server: POST /chat with a hardcoded mock reply, contract version one.
# Run from the PythonProject/ folder:   uvicorn stages.bridge_mock:app --reload --port 8000
# Needs static/audio/response_01.wav (the final server creates it on first start,
# or copy the provided mock WAV there).
from typing import Optional
from fastapi import FastAPI
from fastapi.staticfiles import StaticFiles
from pydantic import BaseModel
from pathlib import Path

Path("static/audio").mkdir(parents=True, exist_ok=True)


class ChatRequest(BaseModel):
    text: str


class ChatResponse(BaseModel):
    speech_text: str
    audio_url: Optional[str] = None


app = FastAPI(title="Virtual Human Server (mock)")
app.mount("/static", StaticFiles(directory="static"), name="static")


@app.get("/")
def root():
    return {"status": "ok", "brain": "mock"}


@app.post("/chat", response_model=ChatResponse)
def chat(req: ChatRequest):
    return ChatResponse(
        speech_text="Hello! I am ready to assist you.",
        audio_url="/static/audio/response_01.wav",
    )
''',
    # ------------------------------------------------------------ models/README.md
    "models/README.md": r'''# models/

The speech models go here. They are too big for Git, so they are not in the repository.

```
models/
  faster-whisper-small/                    speech to text (Whisper small)
  piper-voice-en_US-hfc_female-medium/     English voice (default)
  piper-voice-it_IT-serena-high/           Italian voice (optional, TTS_LANGUAGE=it)
```

Download links and the exact file list are in `Software_&_Downloads.md` (§5.1, at the root of the repository):

| Folder | Files | Source |
|---|---|---|
| `faster-whisper-small/` | `config.json`, `model.bin`, `tokenizer.json`, `vocabulary.txt` | <https://huggingface.co/Systran/faster-whisper-small> → *Files* |
| `piper-voice-en_US-hfc_female-medium/` | `en_US-hfc_female-medium.onnx`, `en_US-hfc_female-medium.onnx.json` | <https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US/hfc_female/medium> |
| `piper-voice-it_IT-serena-high/` | `it_IT-serena-high.onnx`, `it_IT-serena-high.onnx.json` | <https://huggingface.co/rhasspy/piper-voices/tree/main/it/it_IT/serena/high> |

Put the files of each model into a folder with the name shown above.

Or let a script do it: from the repository root run `python download_models.py` (add `--no-italian` to skip the Italian voice).
''',
    # ------------------------------------------------------------ run_server.bat
    "run_server.bat": r'''@echo off
rem VH: starts the Python server (Unity opens this window on Play). Close the window to stop the server.
setlocal
cd /d "%~dp0"
title VH - Python server (close this window to stop it)
set "STARTER=%~dp0..\..\VH_Starter_Project\PythonProject"

rem 1. Python environment: this project's .venv, else the one you made in VH_Starter_Project, else create it.
set "PY=.venv\Scripts\python.exe"
if not exist "%PY%" if exist "%STARTER%\.venv\Scripts\python.exe" set "PY=%STARTER%\.venv\Scripts\python.exe"
if exist "%PY%" goto models
echo First start: creating .venv and installing the requirements. This takes several minutes, only once.
python -m venv .venv
if errorlevel 1 goto nopython
".venv\Scripts\python.exe" -m pip install -r requirements.txt
if errorlevel 1 goto failed
set "PY=.venv\Scripts\python.exe"

:models
rem 2. Speech models: copied from VH_Starter_Project if you have them there, else downloaded once.
if exist "models\faster-whisper-small\model.bin" goto voice
if exist "%STARTER%\models\faster-whisper-small\model.bin" (
  echo Copying the speech models from VH_Starter_Project...
  robocopy "%STARTER%\models" "models" /E /NFL /NDL /NJH /NJS /NP >nul
)
:voice
set "NEED="
if not exist "models\faster-whisper-small\model.bin" set "NEED=1"
if not exist "models\piper-voice-en_US-hfc_female-medium\en_US-hfc_female-medium.onnx" set "NEED=1"
set "DL=..\download_models.py"
if not exist "%DL%" set "DL=..\..\download_models.py"
if defined NEED "%PY%" "%DL%" --no-italian

rem 3. Settings and server.
if not exist .env if exist .env.example copy .env.example .env >nul
echo.
echo Server: http://127.0.0.1:8000   (the Ollama app must be running and signed in, else the brain echoes)
echo Close this window to stop it.
echo.
"%PY%" -m uvicorn main:app --port 8000
echo.
echo The server stopped: read the messages above.
pause
exit /b 0

:nopython
echo.
echo Python was not found. Install Python 3.12 from python.org (tick "Add python.exe to PATH"), then press Play again.
pause
exit /b 1

:failed
echo.
echo Installing the requirements failed: read the error above.
pause
exit /b 1
''',
    # ------------------------------------------------------------ run_server.sh
    "run_server.sh": r'''#!/usr/bin/env bash
# VH: starts the Python server (Unity opens this in a terminal on Play). Close the window or Ctrl+C to stop it.
cd "$(dirname "$0")" || exit 1
STARTER="../../VH_Starter_Project/PythonProject"

# 1. Python environment: this project's .venv, else the one you made in VH_Starter_Project, else create it.
PY=".venv/bin/python"
[ -x "$PY" ] || { [ -x "$STARTER/.venv/bin/python" ] && PY="$STARTER/.venv/bin/python"; }
if [ ! -x "$PY" ]; then
  echo "First start: creating .venv and installing the requirements. This takes several minutes, only once."
  PYTHON=$(command -v python3.12 || command -v python3)
  [ -n "$PYTHON" ] || { echo "Python 3.12 not found: install it from python.org, then press Play again."; read -r; exit 1; }
  "$PYTHON" -m venv .venv && .venv/bin/python -m pip install -r requirements.txt || { echo "Installing the requirements failed."; read -r; exit 1; }
  PY=".venv/bin/python"
fi

# 2. Speech models: copied from VH_Starter_Project if you have them there, else downloaded once.
if [ ! -f models/faster-whisper-small/model.bin ] && [ -f "$STARTER/models/faster-whisper-small/model.bin" ]; then
  echo "Copying the speech models from VH_Starter_Project..."
  cp -R "$STARTER/models/." models/
fi
if [ ! -f models/faster-whisper-small/model.bin ] || [ ! -f models/piper-voice-en_US-hfc_female-medium/en_US-hfc_female-medium.onnx ]; then
  DL=../download_models.py; [ -f "$DL" ] || DL=../../download_models.py
  "$PY" "$DL" --no-italian
fi

# 3. Settings and server.
[ -f .env ] || { [ -f .env.example ] && cp .env.example .env; }
echo
echo "Server: http://127.0.0.1:8000   (the Ollama app must be running and signed in, else the brain echoes)"
echo "Close this window or press Ctrl+C to stop it."
echo
"$PY" -m uvicorn main:app --port 8000
echo "The server stopped: read the messages above. Press Enter to close."
read -r
''',
}

# Files that must keep Windows line endings.
CRLF = {"run_server.bat"}


def find_project(arg: str | None) -> Path:
    if arg:
        p = Path(arg).resolve()
        if p.name != "PythonProject" and (p / "PythonProject").is_dir():
            p = p / "PythonProject"
        if not p.is_dir():
            sys.exit(f"{p} is not a folder.")
        return p
    for c in (HERE / "VH_Starter_Project" / "PythonProject", Path.cwd() / "VH_Starter_Project" / "PythonProject",
              HERE / "PythonProject", Path.cwd() / "PythonProject"):
        if c.is_dir():
            return c
    if (Path.cwd() / "main.py").is_file():
        return Path.cwd()
    sys.exit("VH_Starter_Project/PythonProject not found: put this script in the repository root, "
             "or pass the folder as argument.")


def wanted(rel: str) -> bytes:
    text = FILES[rel]
    if rel in CRLF:
        text = text.replace("\r\n", "\n").replace("\n", "\r\n")
    return text.encode("utf-8")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("project", nargs="?", help="the project or PythonProject folder (default: VH_Starter_Project)")
    ap.add_argument("--dry-run", action="store_true", help="only show what would change")
    ap.add_argument("--yes", "-y", action="store_true", help="do not ask before replacing your files")
    args = ap.parse_args()

    proj = find_project(args.project)
    print(f"PythonProject: {proj}\n")

    plan = []                                    # (rel, "add" | "replace")
    for rel in FILES:
        dst = proj / rel
        if not dst.exists():
            plan.append((rel, "add"))
        elif dst.read_bytes().replace(b"\r\n", b"\n") != wanted(rel).replace(b"\r\n", b"\n"):
            plan.append((rel, "replace"))
    for rel in FILES:
        state = dict(plan).get(rel, "ok")
        print(f"  {state:8} {rel}")

    if not plan:
        print("\nAlready at the webinar starting point. Nothing to do.")
        return
    if args.dry_run:
        print("\nDry run: nothing changed.")
        return

    replaced = [r for r, s in plan if s == "replace"]
    backup = proj / time.strftime("_before_webinar_%Y%m%d-%H%M%S")
    if replaced and not args.yes:
        print(f"\n{len(replaced)} of your files will be replaced; the old versions go to {backup.name}/.")
        if input("Continue? [y/N] ").strip().lower() not in ("y", "yes", "s", "si"):
            print("Nothing changed.")
            return

    for rel, state in plan:
        dst = proj / rel
        if state == "replace":
            (backup / rel).parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(dst, backup / rel)
        dst.parent.mkdir(parents=True, exist_ok=True)
        dst.write_bytes(wanted(rel))
        if rel.endswith(".sh"):
            dst.chmod(0o755)

    (proj / "static" / "audio").mkdir(parents=True, exist_ok=True)
    (proj / "static" / "audio" / ".gitkeep").touch()
    (proj / "models").mkdir(exist_ok=True)

    bad = []
    for rel in FILES:
        if rel.endswith(".py"):
            try:
                py_compile.compile(str(proj / rel), doraise=True, cfile=str(proj / "__pycache__" / "_check.pyc"))
            except py_compile.PyCompileError as e:
                bad.append(f"{rel}: {e.msg}")
    shutil.rmtree(proj / "__pycache__" / "_check.pyc", ignore_errors=True)
    (proj / "__pycache__" / "_check.pyc").unlink(missing_ok=True)

    print(f"\nDone: {len(plan)} file(s) updated." + (f" Your old versions are in {backup.name}/." if replaced else ""))
    if bad:
        print("Syntax check FAILED:\n  " + "\n  ".join(bad))
        sys.exit(1)
    print("Next: open the VH_Webinar_Starter scene in Unity and start the server with "
          "Masterclass -> Python Server -> Start Now (or run run_server.bat in PythonProject).")
    print("The Ollama app must be running and signed in, else the brain echoes.")


if __name__ == "__main__":
    main()
