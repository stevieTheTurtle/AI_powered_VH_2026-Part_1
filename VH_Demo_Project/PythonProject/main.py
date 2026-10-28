# main.py — Server: the bridge between the brain side (Python) and the body (Unity)   (demo: every gap filled, Day 1-2 and webinar)
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
