# try_pipeline.py — the whole brain side, no Unity, no server.
#   python try_pipeline.py Recording.m4a   (record it with Windows Sound Recorder; WAV works too)
import asyncio, sys, time
from stt import transcribe
from brain import think_text
from tts import synthesize

# TURN-2 (solution)
t0 = time.perf_counter(); heard = transcribe(sys.argv[1])
t1 = time.perf_counter(); reply = think_text(heard)
t2 = time.perf_counter(); audio = asyncio.run(synthesize(reply))
t3 = time.perf_counter()

print("Heard:", heard)
print("Reply:", reply)
print("Audio:", audio)
print(f"stt {t1-t0:.2f} s · brain {t2-t1:.2f} s · tts {t3-t2:.2f} s")
