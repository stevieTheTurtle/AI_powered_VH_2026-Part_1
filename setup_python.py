"""Set up the Python side of every course project in one go: .venv, packages, speech models, .env.

Run it from the repository root (the folder that holds VH_Starter_Project, VH_Demo_Project, ...),
next to download_models.py and init_webinar.py:

    python setup_python.py                          # every project folder found here
    python setup_python.py VH_Starter_Project       # only the projects you name
    python setup_python.py --no-italian             # skip the optional Italian voice
    python setup_python.py --no-models              # packages and .env only
    python setup_python.py --recreate               # delete and rebuild the .venv
    python setup_python.py --check                  # only report what is missing, change nothing

A project is any folder next to this script with PythonProject/requirements.txt inside.
For each one it does what run_server.bat does on first start, without starting the server:

1. PythonProject/.venv with Python 3.12 (found with the py launcher on Windows, python3.12 elsewhere).
2. pip install -r requirements.txt (skipped when requirements.txt has not changed since the last install).
3. Speech models in PythonProject/models/: copied from a project that already has them,
   otherwise downloaded once with download_models.py.
4. .env copied from .env.example if there is none (an existing .env is never touched).
5. A quick import test of the installed packages.

Run it again at any time: finished steps are skipped.
"""
import argparse
import hashlib
import os
import shutil
import subprocess
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent
WANTED = (3, 12)
WIN = os.name == "nt"

# A models folder counts as complete when these files are there (same check as run_server.bat).
REQUIRED_MODELS = [
    "faster-whisper-small/model.bin",
    "piper-voice-en_US-hfc_female-medium/en_US-hfc_female-medium.onnx",
    "piper-voice-en_US-hfc_female-medium/en_US-hfc_female-medium.onnx.json",
]
ITALIAN_MODEL = "piper-voice-it_IT-serena-high/it_IT-serena-high.onnx"
# Imported after install to prove the .venv works (module names, not pip names).
IMPORT_TEST = "import fastapi, uvicorn, multipart, pydantic, faster_whisper, piper, dotenv, ollama"
STAMP = ".requirements.sha256"


# ----------------------------------------------------------------- helpers
def say(msg: str = "") -> None:
    print(msg, flush=True)


def run(cmd: list, **kw) -> int:
    say("    $ " + " ".join(str(c) for c in cmd))
    return subprocess.call([str(c) for c in cmd], **kw)


def venv_python(py_dir: Path) -> Path:
    return py_dir / ".venv" / ("Scripts/python.exe" if WIN else "bin/python")


def version_of(cmd: list) -> tuple | None:
    try:
        out = subprocess.run(cmd + ["-c", "import sys; print(*sys.version_info[:2])"],
                             capture_output=True, text=True, timeout=20)
    except (OSError, subprocess.TimeoutExpired):
        return None
    if out.returncode != 0:
        return None
    try:
        return tuple(int(x) for x in out.stdout.split())
    except ValueError:
        return None


def find_python312() -> list | None:
    """A command that starts Python 3.12: this interpreter, the py launcher, or python3.12."""
    if sys.version_info[:2] == WANTED:
        return [sys.executable]
    candidates = [["py", "-3.12"]] if WIN else [["python3.12"]]
    candidates += [["python3.12"], ["python"], ["python3"]]
    for c in candidates:
        if version_of(c) == WANTED:
            return c
    return None


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def models_complete(models: Path, italian: bool) -> bool:
    need = REQUIRED_MODELS + ([ITALIAN_MODEL] if italian else [])
    return all((models / f).is_file() and (models / f).stat().st_size > 0 for f in need)


def find_projects(names: list[str]) -> list[Path]:
    if names:
        projects = []
        for n in names:
            p = (ROOT / n).resolve()
            if not (p / "PythonProject" / "requirements.txt").is_file():
                sys.exit(f"{n}: no PythonProject/requirements.txt inside, is the name right?")
            projects.append(p)
        return projects
    found = sorted(p for p in ROOT.iterdir()
                   if p.is_dir() and (p / "PythonProject" / "requirements.txt").is_file())
    if not found:
        sys.exit("No project found: put this script in the folder that holds the *_Project folders.")
    # The starter first: its .venv and models are the ones the others copy from.
    found.sort(key=lambda p: (p.name != "VH_Starter_Project", p.name.lower()))
    return found


# ----------------------------------------------------------------- steps
def step_venv(py_dir: Path, python312: list | None, recreate: bool, check: bool) -> bool:
    venv = py_dir / ".venv"
    vpy = venv_python(py_dir)
    if recreate and venv.exists() and not check:
        say("  .venv: deleting (--recreate)")
        shutil.rmtree(venv)
    if vpy.exists():
        v = version_of([str(vpy)])
        if v == WANTED:
            say("  .venv: ok (Python 3.12)")
            return True
        say(f"  .venv: Python {'.'.join(map(str, v)) if v else '?'} instead of 3.12"
            + ("" if check else ", rebuild it with --recreate"))
        return not check and v is not None   # usable, but warn
    if check:
        say("  .venv: MISSING")
        return False
    if python312 is None:
        say("  .venv: cannot create it, Python 3.12 not found. Install it from python.org "
            "(tick 'Add python.exe to PATH') and run this script again.")
        return False
    say("  .venv: creating")
    return run(python312 + ["-m", "venv", venv]) == 0 and vpy.exists()


def step_packages(py_dir: Path, check: bool) -> bool:
    req = py_dir / "requirements.txt"
    vpy = venv_python(py_dir)
    stamp = py_dir / ".venv" / STAMP
    current = sha256(req)
    if stamp.is_file() and stamp.read_text().strip() == current:
        say("  packages: ok (requirements.txt unchanged since the last install)")
        return True
    if check:
        say("  packages: NOT INSTALLED or requirements.txt changed")
        return False
    if not vpy.exists():
        return False
    say("  packages: installing (several minutes the first time)")
    run([vpy, "-m", "pip", "install", "--upgrade", "pip", "--quiet"])
    if run([vpy, "-m", "pip", "install", "-r", req], cwd=py_dir) != 0:
        say("  packages: pip FAILED, read the error above")
        return False
    stamp.write_text(current)
    return True


def step_models(py_dir: Path, sources: list[Path], italian: bool, check: bool) -> bool:
    models = py_dir / "models"
    if models_complete(models, italian):
        say("  models: ok")
        return True
    if check:
        say("  models: MISSING (Whisper small and/or the Piper voice)")
        return False
    models.mkdir(exist_ok=True)
    for src in sources:
        if src != models and models_complete(src, italian):
            say(f"  models: copying from {src.relative_to(ROOT)}")
            for d in src.iterdir():
                if d.is_dir():
                    shutil.copytree(d, models / d.name, dirs_exist_ok=True,
                                    ignore=shutil.ignore_patterns("*.part"))
            return models_complete(models, italian)
    downloader = ROOT / "download_models.py"
    if not downloader.is_file():
        say("  models: download_models.py not found next to this script, download them by hand "
            "(see models/README.md)")
        return False
    say("  models: downloading (about 550 MB the first time)")
    cmd = [sys.executable, downloader, "--dest", models] + ([] if italian else ["--no-italian"])
    run(cmd)
    return models_complete(models, italian)


def step_env(py_dir: Path, check: bool) -> bool:
    env, example = py_dir / ".env", py_dir / ".env.example"
    if env.is_file():
        say("  .env: ok (left as it is)")
        return True
    if not example.is_file():
        say("  .env: no .env.example to copy, skipped")
        return True
    if check:
        say("  .env: MISSING")
        return False
    shutil.copyfile(example, env)
    say("  .env: created from .env.example")
    return True


def step_folders(py_dir: Path, check: bool) -> None:
    audio = py_dir / "static" / "audio"
    if not audio.is_dir() and not check:
        audio.mkdir(parents=True, exist_ok=True)


def step_test(py_dir: Path) -> bool:
    vpy = venv_python(py_dir)
    if not vpy.exists():
        return False
    ok = subprocess.run([str(vpy), "-c", IMPORT_TEST], cwd=py_dir,
                        capture_output=True, text=True)
    if ok.returncode == 0:
        say("  import test: ok")
        return True
    say("  import test: FAILED\n    " + (ok.stderr.strip().splitlines() or ["?"])[-1])
    return False


# ----------------------------------------------------------------- main
def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    ap.add_argument("projects", nargs="*", help="project folders to set up (default: all found here)")
    ap.add_argument("--no-italian", action="store_true", help="skip the optional Italian Piper voice")
    ap.add_argument("--no-models", action="store_true", help="do not copy or download the speech models")
    ap.add_argument("--recreate", action="store_true", help="delete and rebuild each .venv")
    ap.add_argument("--check", action="store_true", help="only report what is missing, change nothing")
    args = ap.parse_args()

    projects = find_projects(args.projects)
    italian = not args.no_italian
    python312 = None if args.check else find_python312()
    if not args.check:
        say(f"Python 3.12: {' '.join(python312) if python312 else 'NOT FOUND'}")
    # Where to copy models from: every project in the folder, so a named one can still copy.
    sources = [p / "PythonProject" / "models"
               for p in find_projects([]) if p.is_dir()]

    results = {}
    for proj in projects:
        py_dir = proj / "PythonProject"
        say(f"\n== {proj.name} ==")
        ok = step_venv(py_dir, python312, args.recreate, args.check)
        ok = step_packages(py_dir, args.check) and ok if ok or args.check else False
        if not args.no_models:
            ok = step_models(py_dir, sources, italian, args.check) and ok
        ok = step_env(py_dir, args.check) and ok
        step_folders(py_dir, args.check)
        if not args.check and ok:
            ok = step_test(py_dir) and ok
        results[proj.name] = ok

    say("\n== Summary ==")
    for name, ok in results.items():
        say(f"  {'READY  ' if ok else 'TO FIX '} {name}")
    if all(results.values()) and not args.check:
        say("\nStart a server with:  cd <project>/PythonProject  then  "
            + (r".venv\Scripts\python -m uvicorn main:app --port 8000" if WIN
               else ".venv/bin/python -m uvicorn main:app --port 8000"))
        say("The Ollama app must be running and signed in, else the brain echoes.")
    sys.exit(0 if all(results.values()) else 1)


if __name__ == "__main__":
    main()
