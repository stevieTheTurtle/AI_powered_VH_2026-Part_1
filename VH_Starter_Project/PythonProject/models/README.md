# models/

The speech models go here. They are too big for Git, so they are not in the repository.

```
models/
  faster-whisper-small/                    speech to text (Whisper small)
  piper-voice-en_US-hfc_female-medium/     English voice (default)
  piper-voice-it_IT-serena-high/           Italian voice (optional, TTS_LANGUAGE=it)
```

Download links and the exact file list are in `Software_&_Downloads.md` (§5.1, at the root of the repository):

| Folder | Files | Source |
|---|---|---|
| `faster-whisper-small/` | `config.json`, `model.bin`, `tokenizer.json`, `vocabulary.txt` | <https://huggingface.co/Systran/faster-whisper-small> → *Files* |
| `piper-voice-en_US-hfc_female-medium/` | `en_US-hfc_female-medium.onnx`, `en_US-hfc_female-medium.onnx.json` | <https://huggingface.co/rhasspy/piper-voices/tree/main/en/en_US/hfc_female/medium> |
| `piper-voice-it_IT-serena-high/` | `it_IT-serena-high.onnx`, `it_IT-serena-high.onnx.json` | <https://huggingface.co/rhasspy/piper-voices/tree/main/it/it_IT/serena/high> |

Put the files of each model into a folder with the name shown above.

Or let a script do it: from the repository root run `python download_models.py` (add `--no-italian` to skip the Italian voice).
