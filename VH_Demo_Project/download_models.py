"""Download the speech models for the course into VH_Starter_Project/PythonProject/models/.

Run it from the repository root (the folder that contains VH_Starter_Project):

    python download_models.py               # Whisper small + English voice + Italian voice
    python download_models.py --no-italian  # skip the optional Italian voice

Only the Python standard library is used, so it works before the .venv exists.
Files that are already complete are skipped, so you can run it again after an interruption.
Dropped connections are retried, and a partial download resumes where it stopped.
"""
import argparse
import hashlib
import sys
import time
import urllib.error
import urllib.request
from pathlib import Path

HF = "https://huggingface.co"
HEADERS = {"User-Agent": "aivh-course"}
ATTEMPTS = 6          # per request; waits 2, 4, 8, 16, 30 s between attempts

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


def retry(what: str, fn, *args):
    """Call fn(*args); on a network error wait and try again (Wi-Fi drops, resets, timeouts)."""
    for attempt in range(1, ATTEMPTS + 1):
        try:
            return fn(*args)
        except urllib.error.HTTPError as e:
            if e.code < 500 and e.code != 429:   # 404 and similar: retrying will not help
                raise
            err = e
        except (urllib.error.URLError, ConnectionError, TimeoutError, OSError) as e:
            err = e
        if attempt == ATTEMPTS:
            raise err
        wait = min(2 ** attempt, 30)
        print(f"    {what}: {getattr(err, 'reason', err)}, retrying in {wait} s ({attempt}/{ATTEMPTS - 1})")
        time.sleep(wait)


def remote_info(url: str) -> tuple[int, str | None, str]:
    """Size in bytes, the SHA-256 Hugging Face publishes for large (LFS) files, and the URL to download.

    Large files redirect to the Hugging Face CDN. Small text files (config.json, .onnx.json...)
    would redirect to huggingface.co/api/resolve-cache/..., which some networks reset, so they
    are fetched from the plain /raw/ address instead.
    """
    req = urllib.request.Request(url, method="HEAD", headers=HEADERS)
    opener = urllib.request.build_opener(_NoRedirect)
    try:
        with opener.open(req, timeout=30) as r:
            headers = r.headers
    except urllib.error.HTTPError as e:   # the redirect arrives here
        if e.code not in (301, 302, 303, 307, 308):
            raise
        headers = e.headers
    size = headers.get("X-Linked-Size")
    if size is not None:   # large file: the CDN link, checked with SHA-256
        sha = (headers.get("X-Linked-ETag") or "").strip('"')
        return int(size), (sha if len(sha) == 64 else None), url
    raw = url.replace("/resolve/", "/raw/", 1)
    req = urllib.request.Request(raw, method="HEAD", headers=HEADERS)
    with urllib.request.urlopen(req, timeout=30) as r:
        return int(r.headers["Content-Length"]), None, raw


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
    done = part.stat().st_size if part.exists() else 0
    if done > size:
        part.unlink()
        done = 0
    if done < size:
        headers = dict(HEADERS, Range=f"bytes={done}-") if done else HEADERS
        req = urllib.request.Request(url, headers=headers)
        with urllib.request.urlopen(req, timeout=60) as r:
            if done and r.status != 206:   # server ignored the range: start over
                done = 0
            with open(part, "ab" if done else "wb") as out:
                _copy(r, out, done, size)
    print()
    if part.stat().st_size != size:
        raise ConnectionError(f"incomplete download ({part.stat().st_size} of {size} bytes)")
    part.replace(dest)


def _copy(r, out, done: int, size: int) -> None:
    last = 0.0
    while block := r.read(1 << 20):
        out.write(block)
        done += len(block)
        if time.time() - last > 0.5 or done == size:
            last = time.time()
            print(f"\r    {done / 1e6:7.1f} / {size / 1e6:.1f} MB", end="", flush=True)


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
                size, sha, src = retry(name, remote_info, url)
                if dest.exists() and dest.stat().st_size == size:
                    print(f"  {name}: already there")
                    continue
                print(f"  {name}: downloading")
                retry(name, download, src, dest, size)
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
