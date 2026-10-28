# brain.py — Brain: THE SEAM. think(text) -> BrainOutput        (demo: every gap filled, Day 1-2 and webinar)
#
#   think_text()        one LLM call, text in, text out                       (LIVE-3)
#   think_structured()  the same call, returning JSON that also chooses a gesture and an emotion (LIVE-8)
#   No Ollama app:      echo (it repeats what you said), so nothing breaks in front of the class
#   Part 2:             replace the body of think() with an agent. Nothing else changes.
#   Reference only:     BRAIN=text in .env makes think() text-only, to demo the voice-loop checkpoint.
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
# TURN-6: the four "- ACTION: when" lines after "ACTION must be exactly one of"; EXAMPLES is the webinar's prompt-tuning step (8.8)
SYSTEM_PROMPT = """You are Camila, a friendly virtual assistant who lives inside a 3D scene and talks with a person face to face.

STYLE
- Answer in one or two short sentences, in the language of the user.
- Warm, natural and spoken; no lists, no markdown, no emojis.

OUTPUT
Reply with ONE JSON object and nothing else:
{"speech_text": "<what you say>", "action": "<ACTION>", "emotion": "<EMOTION>"}

ACTION must be exactly one of: NONE, WAVE, SHRUG, THINKING
- WAVE: greetings and goodbyes
- SHRUG: you don't know or are unsure
- THINKING: the question is hard or open-ended
- NONE: otherwise

EMOTION must be exactly one of: NEUTRAL, HAPPY, SAD
Choose the emotion that fits your reply. Bad news or sad topics: SAD, usually with NONE.
Never use an action or emotion that is not in the lists.

EXAMPLES
User: Hi there!
{"speech_text": "Hi! Great to see you.", "action": "WAVE", "emotion": "HAPPY"}
User: My dog died yesterday.
{"speech_text": "I'm so sorry. That must be really hard.", "action": "NONE", "emotion": "SAD"}
User: Do you know what the stock market will do tomorrow?
{"speech_text": "I really can't say, that's impossible to predict.", "action": "SHRUG", "emotion": "NEUTRAL"}"""

# The Ollama app runs locally (http://localhost:11434). Signed in with a free ollama.com
# account, it also serves cloud models (names ending in -cloud): they run on Ollama's servers
# on the free quota, so no GPU and no key in .env. A model you pulled runs on your machine.
# Without the app: LLM_HOST=https://ollama.com plus OLLAMA_API_KEY calls the cloud directly.
# (Not OLLAMA_HOST: Ollama installs often set that machine-wide for the local server.)
_MODE = os.getenv("BRAIN", "structured").lower()          # reference only: text | structured
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


# Demo only: what the last call to the brain produced, for the HUD's brain debugger (GET /brain-debug).
last_debug = {"user_text": "", "status": "no call yet", "raw": "", "error": ""}


def _debug(user_text: str, status: str, raw: str = "", error: str = ""):
    last_debug.update(user_text=user_text, status=status, raw=raw or "", error=str(error)[:600])


def brain_name() -> str:
    if _client is None:
        return "echo"
    return "llm-text" if _MODE == "text" else "llm"


def think_text(user_text: str) -> str:
    """Text in, what to say out."""
    if _client is None:
        return user_text                                 # echo: Ollama app not running
    # LIVE-3
    try:
        resp = _client.chat(model=_MODEL, messages=[
            {"role": "system", "content": TEXT_PROMPT},
            {"role": "user", "content": user_text}])
        return resp.message.content.strip()
    except Exception as e:                               # network, auth, quota used up...
        print("Brain: LLM call failed:", e)
        return user_text


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
        _debug(user_text, "echo: no LLM, the brain repeats your words")
        return BrainOutput(speech_text=user_text, action="THINKING")   # echo, with a visible gesture
    # LIVE-8
    raw = ""
    try:
        # Schema-constrained output: Ollama asks the model for exactly this structure.
        # Pydantic still validates it, because "reduces failures" is not "eliminates".
        resp = _client.chat(
            model=_MODEL,
            messages=[{"role": "system", "content": SYSTEM_PROMPT},
                      {"role": "user", "content": user_text}],
            format=BrainOutput.model_json_schema(),
            options={"temperature": _TEMPERATURE})
        raw = resp.message.content
        out = _parse(raw)
        _debug(user_text, "valid: fits the schema", raw)
        return out
    except (ValidationError, ValueError) as e:
        print("Brain: invalid JSON from model:", e)
        _debug(user_text, "invalid: does not fit the schema -> fallback", raw, e)
        return _fallback()
    except Exception as e:                               # network, auth, quota used up...
        print("Brain: LLM call failed:", e)
        _debug(user_text, "LLM call failed -> fallback", raw, e)
        return _fallback()


def think(user_text: str) -> BrainOutput:
    """THE SEAM: the server only ever calls this."""
    if _MODE == "text":                                  # reference only: voice-loop checkpoint
        out = BrainOutput(speech_text=think_text(user_text))
        _debug(user_text, "text only (BRAIN=text): no JSON", out.speech_text)
        return out
    return think_structured(user_text)


if __name__ == "__main__":                               # python brain.py
    print("Brain:", brain_name())
    while True:
        print("Camila:", think(input("You: ")))
