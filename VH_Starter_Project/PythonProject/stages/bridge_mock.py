# Fallback server: POST /chat with a hardcoded mock reply, contract version one.
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
