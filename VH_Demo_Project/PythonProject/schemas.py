# schemas.py — the JSON contract between brain and body
from typing import Dict, Literal, Optional
from pydantic import BaseModel, ConfigDict

# The body's vocabulary: what the brain is allowed to ask for.
Action = Literal["NONE", "WAVE", "SHRUG", "THINKING"]
Emotion = Literal["NEUTRAL", "HAPPY", "SAD", "SURPRISED", "ANGRY"]


class ChatRequest(BaseModel):
    text: str


class BrainOutput(BaseModel):
    # extra="forbid" -> "additionalProperties": false in the JSON schema sent to the LLM
    model_config = ConfigDict(extra="forbid")

    speech_text: str
    # ===== TURN-5 · Contract ▸ body language =====
    # Add two fields with safe defaults: `action` (an Action, default "NONE")
    # and `emotion` (an Emotion, default "NEUTRAL"). Unity already reads both names.
    # ===== end TURN-5 =====


class ChatResponse(BrainOutput):
    model_config = ConfigDict(extra="ignore")

    audio_url: Optional[str] = None
    transcript: Optional[str] = None
    timings_ms: Optional[Dict[str, int]] = None
    brain: Optional[str] = None      # "echo" or "llm": shown in the console
