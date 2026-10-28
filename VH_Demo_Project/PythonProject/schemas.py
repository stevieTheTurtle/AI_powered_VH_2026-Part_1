# schemas.py — the JSON contract between brain and body
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
    # TURN-5
    action: Action = "NONE"
    emotion: Emotion = "NEUTRAL"


class ChatResponse(BrainOutput):
    model_config = ConfigDict(extra="ignore")

    audio_url: Optional[str] = None
    transcript: Optional[str] = None
    timings_ms: Optional[Dict[str, int]] = None
    brain: Optional[str] = None      # "echo", "llm" or "llm-text": shown in the HUD
