#!/usr/bin/env bash
# VH Demo: starts the Python server (Unity opens this in a terminal on Play). Close the window or Ctrl+C to stop it.
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
  "$PY" ../download_models.py --no-italian
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
