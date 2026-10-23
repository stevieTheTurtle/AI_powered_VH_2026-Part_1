@echo off
rem VH Demo: starts the Python server (Unity opens this window on Play). Close the window to stop the server.
setlocal
cd /d "%~dp0"
title VH Demo - Python server (close this window to stop it)
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
if defined NEED "%PY%" ..\download_models.py --no-italian

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
