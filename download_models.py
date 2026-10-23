"""Download the speech models for the course into VH_Starter_Project/PythonProject/models/.

Run it from the repository root (the folder that contains VH_Starter_Project):

    python download_models.py               # Whisper small + English voice + Italian voice
    python download_models.py --no-italian  # skip the optional Italian voice

Only the Python standard library is used, so it works before the .venv exists.
Files that are already complete are skipped, so you can run it again after an interruption.
"""
import argparse
import hashlib
import sys
import time
import urllib.request
from pathlib import Path

HF = "https://huggingface.co"

# folder name -> (Hugging Face repo, path inside the repo, file names)
MODELS = {
    "faster-whisper-small": (
        "Systran/faster-whisper-small", "",
        ["config.json", "model.bin", "tokenizer.json", "vocabulary.txt"]),
    "piper-voice-en_US-hfc_female-medium": (
        "rhasspy/piper-voices", "en/en_US/hfc_female/medium/",
        ["en_US-hfc_female-medium.onnx", "en_US-hfc_female-medium.onnx.json"]),
    "piper-voice-it_IT-serena-high": (
        "rhasspy/piper-voices", "it/it_IT/serena/high/",
        ["it_IT-serena-high.onnx", "it_IT-serena-high.onnx.json"]),
}
OPTIONAL = {"piper-voice-it_IT-serena-high"}


def find_models_dir() -> Path:
    here = Path(__file__).resolve().parent
    for candidate in (here / "VH_Starter_Project" / "PythonProject" / "models",
                      here / "PythonProject" / "models",
                      here / "models"):
        if candidate.parent.is_dir():
            return candidate
    sys.exit("Could not find VH_Starter_Project/PythonProject/. Put this script in the repository root.")


def remote_info(url: str) -> tuple[int, str | None]:
    """Size in bytes and, for large (LFS) files, the SHA-256 that Hugging Face publishes."""
    req = urllib.request.Request(url, method="HEAD", headers={"User-Agent": "aivh-course"})
    opener = urllib.request.build_opener(_NoRedirect)
    try:
        with opener.open(req, timeout=30) as r:
            headers = r.headers
    except urllib.error.HTTPError as e:   # the redirect to the CDN arrives here
        if e.code not in (301, 302, 303, 307, 308):
            raise
        headers = e.headers
    sha = (headers.get("X-Linked-ETag") or "").strip('"') or None
    if sha and len(sha) != 64:
        sha = None
    size = headers.get("X-Linked-Size")
    if size is None:   # small file: follow the redirect and read the real size
        req = urllib.request.Request(url, method="HEAD", headers={"User-Agent": "aivh-course"})
        with urllib.request.urlopen(req, timeout=30) as r:
            size = r.headers.get("Content-Length")
    return int(size), sha


class _NoRedirect(urllib.request.HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        return None


def sha256(path: Path) -> str:
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for block in iter(lambda: f.read(1 << 20), b""):
            h.update(block)
    return h.hexdigest()


def download(url: str, dest: Path, size: int) -> None:
    part = dest.with_name(dest.name + ".part")
    req = urllib.request.Request(url, headers={"User-Agent": "aivh-course"})
    with urllib.request.urlopen(req, timeout=60) as r, open(part, "wb") as out:
        done, last = 0, 0.0
        while block := r.read(1 << 20):
            out.write(block)
            done += len(block)
            if time.time() - last > 0.5 or done == size:
                last = time.time()
                print(f"\r    {done / 1e6:7.1f} / {size / 1e6:.1f} MB", end="", flush=True)
    print()
    if part.stat().st_size != size:
        raise RuntimeError(f"incomplete download ({part.stat().st_size} of {size} bytes)")
    part.replace(dest)


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("--no-italian", action="store_true", help="skip the optional Italian Piper voice")
    args = ap.parse_args()

    models_dir = find_models_dir()
    print(f"Models folder: {models_dir}")
    failed = []
    for folder, (repo, sub, files) in MODELS.items():
        if args.no_italian and folder in OPTIONAL:
            continue
        print(f"\n{folder}")
        target = models_dir / folder
        target.mkdir(parents=True, exist_ok=True)
        for name in files:
            url = f"{HF}/{repo}/resolve/main/{sub}{name}"
            dest = target / name
            try:
                size, sha = remote_info(url)
                if dest.exists() and dest.stat().st_size == size:
                    print(f"  {name}: already there")
                    continue
                print(f"  {name}: downloading")
                download(url, dest, size)
                if sha and sha256(dest) != sha:
                    dest.unlink()
                    raise RuntimeError("checksum mismatch, file removed")
            except Exception as e:  # keep going with the other files, report at the end
                print(f"  {name}: FAILED ({e})")
                failed.append(f"{folder}/{name}")

    print()
    if failed:
        print("Some files failed: " + ", ".join(failed))
        print("Run the script again, or download them by hand (links in PythonProject/models/README.md).")
        sys.exit(1)
    print("All models are in place.")


if __name__ == "__main__":
    main()
