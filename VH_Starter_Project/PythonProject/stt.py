# stt.py — Ears: speech to text with faster-whisper (local model)
import os, sys
from pathlib import Path
import ctranslate2
from faster_whisper import WhisperModel

# Local model folder only: no download from Hugging Face.
# Default: PythonProject/models/faster-whisper-small. Override with WHISPER_MODEL=<folder>.
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
