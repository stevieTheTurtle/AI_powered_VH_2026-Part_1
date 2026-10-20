# try_pipeline.py — the whole brain side, no Unity, no server.
#   python try_pipeline.py Recording.m4a   (record it with Windows Sound Recorder; WAV works too)
import asyncio, sys, time
from stt import transcribe
from brain import think_text
from tts import synthesize

# ===== TURN-2 · Ears ▸ Brain ▸ Voice · the pipeline in three calls =====
# Connect the three modules: the WAV in sys.argv[1] -> transcribe -> think_text -> synthesize.
# synthesize is async: call it with asyncio.run(...). Keep the four timestamps t0..t3
# around the three calls, so the print below shows how long each stage takes.
t0 = t1 = t2 = t3 = time.perf_counter()
heard, reply, audio = "", "", ""                       # placeholder: replace with the three calls
# ===== end TURN-2 =====

print("Heard:", heard)
print("Reply:", reply)
print("Audio:", audio)
print(f"stt {t1-t0:.2f} s · brain {t2-t1:.2f} s · tts {t3-t2:.2f} s")
