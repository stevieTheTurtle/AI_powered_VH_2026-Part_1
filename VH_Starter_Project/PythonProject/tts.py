# tts.py — Voice: local text to speech with Piper, WAV for Unity
import asyncio, os, sys, threading, time, uuid, wave
from pathlib import Path

from piper import PiperVoice
from starlette.concurrency import run_in_threadpool

AUDIO_DIR = Path("static/audio")
AUDIO_DIR.mkdir(parents=True, exist_ok=True)
KEEP = {"response_01.wav"}                             # the bridge mock clip is never cleaned

# Local voices only: no network. TTS_LANGUAGE (en or it, default en) picks the voice, which lives in
# PythonProject/models/. TTS_MODEL=<path to a .onnx file> overrides it (its .onnx.json must sit next to it).
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
