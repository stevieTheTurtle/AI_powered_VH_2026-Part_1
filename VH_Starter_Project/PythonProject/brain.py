# brain.py — Brain: THE SEAM. think(text) -> BrainOutput
#
#   think_text()        one LLM call, text in, text out
#   think_structured()  the same call, returning JSON that also chooses a gesture and an emotion
#   No Ollama app:      echo (it repeats what you said), so nothing breaks in front of the class
#   Part 2:             replace the body of think() with an agent. Nothing else changes.
import os, re
from dotenv import load_dotenv
from pydantic import ValidationError
from schemas import BrainOutput

load_dotenv()                                            # reads PythonProject/.env (also when run alone)


# ---------------------------------------------------------------- text only
TEXT_PROMPT = """You are Camila, a friendly virtual assistant who lives
inside a 3D scene and talks with a person face to face.
Answer in one or two short sentences, in the language of the user.
Warm, natural and spoken; no lists, no markdown, no emojis."""


# ---------------------------------------------------------------- structured (body language)
SYSTEM_PROMPT = """You are Camila, a friendly virtual assistant who lives inside a 3D scene and talks with a person face to face.

STYLE
- Answer in one or two short sentences, in the language of the user.
- Warm, natural and spoken; no lists, no markdown, no emojis.

OUTPUT
Reply with ONE JSON object and nothing else:
{"speech_text": "<what you say>", "action": "<ACTION>", "emotion": "<EMOTION>"}

ACTION must be exactly one of: NONE, WAVE, SHRUG, THINKING
<TURN-6: one line per action saying WHEN to use it, e.g. "- WAVE: greetings and goodbyes">

EMOTION must be exactly one of: NEUTRAL, HAPPY, SAD, SURPRISED, ANGRY
Choose the emotion that fits your reply. Bad news or sad topics: SAD, usually with NONE.
Never use an action or emotion that is not in the lists."""

# The Ollama app runs locally (http://localhost:11434). Signed in with a free ollama.com
# account, it also serves cloud models (names ending in -cloud): they run on Ollama's servers
# on the free quota, so no GPU and no key in .env. A model you pulled runs on your machine.
# Without the app: LLM_HOST=https://ollama.com plus OLLAMA_API_KEY calls the cloud directly.
# (Not OLLAMA_HOST: Ollama installs often set that machine-wide for the local server.)
_KEY = os.getenv("OLLAMA_API_KEY")
_HOST = os.getenv("LLM_HOST", "http://localhost:11434")
_LOCAL = "localhost" in _HOST or "127.0.0.1" in _HOST
_MODEL = os.getenv("LLM_MODEL", "gpt-oss:120b-cloud")
_TEMPERATURE = float(os.getenv("LLM_TEMPERATURE", "0.3"))  # low = less inventive

_client = None
if _KEY or _LOCAL:
    from ollama import Client
    _client = Client(host=_HOST,
                     headers={"Authorization": "Bearer " + _KEY} if _KEY else None)
    if _LOCAL:
        try:
            _client.list()                                # is the Ollama app running?
        except Exception:
            print("Brain: Ollama app not reachable at", _HOST, "-> echo mode")
            _client = None


def brain_name() -> str:
    return "echo" if _client is None else "llm"


def think_text(user_text: str) -> str:
    """Text in, what to say out."""
    if _client is None:
        return user_text                                 # echo: Ollama app not running
    # ===== LIVE-3 · Brain ▸ LLM · one call, text in, text out =====
    # Connects the user's words to the model: messages = the system prompt (TEXT_PROMPT)
    # + the user's text; _client.chat(model=_MODEL, messages=...); return the reply text.
    # Any exception (network, key, quota): print it and fall back to the echo.
    return user_text                                     # placeholder: echo
    # ===== end LIVE-3 =====


def _parse(raw: str) -> BrainOutput:
    """The model's JSON -> a validated BrainOutput (raises if it does not fit the contract)."""
    raw = raw.strip()
    raw = re.sub(r"^```(?:json)?\s*|\s*```$", "", raw)   # strip a code fence if present
    return BrainOutput.model_validate_json(raw)


def _fallback() -> BrainOutput:
    """What the body does when the brain fails: a shrug, never a crash."""
    return BrainOutput(speech_text="Sorry, I lost my train of thought. Could you say that again?",
                       action="SHRUG", emotion="NEUTRAL")


def think_structured(user_text: str) -> BrainOutput:
    """The same single call, now returning speech + action + emotion."""
    if _client is None:
        return BrainOutput(speech_text=user_text)        # echo
    # ===== LIVE-8 · Brain ▸ LLM ▸ Contract · structured output =====
    # Connects the model to the contract: the same _client.chat call, with SYSTEM_PROMPT,
    # format=BrainOutput.model_json_schema() and options={"temperature": _TEMPERATURE};
    # then _parse(reply). Invalid JSON (ValidationError, ValueError) or any other failure
    # -> print it and return _fallback().
    return BrainOutput(speech_text=think_text(user_text))   # placeholder: text only
    # ===== end LIVE-8 =====


def think(user_text: str) -> BrainOutput:
    """THE SEAM: the server only ever calls this."""
    # Text only for now. Body language: return think_structured(user_text) instead.
    return BrainOutput(speech_text=think_text(user_text))


if __name__ == "__main__":                               # python brain.py
    print("Brain:", brain_name())
    while True:
        print("Camila:", think(input("You: ")))
