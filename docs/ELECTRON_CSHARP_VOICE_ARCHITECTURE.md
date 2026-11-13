# Electron + C# voice boundary

Skai Desktop uses Electron/React for the UI and the existing C# agent host for
local desktop capabilities. Voice uses the same validated JSON-lines bridge as
the rest of the desktop app:

```text
React renderer
  └─ window.serverRockAgent.request("voice.*")
       └─ Electron main-process IPC allowlist
            └─ Skai Desktop C# agent host
                 └─ Windows Speech or Linux espeak-ng, espeak, or spd-say
```

The C# host provides:

- `voice.models` — the installed OS-native default and catalog entries for
  optional Kokoro and Pocket TTS model packs.
- `voice.speak` — begins OS-native, interruptible speech for bounded text.
- `voice.stop` — immediately ends the current native speech process.

The renderer owns the selected voice profile, primary model, and low-resource
model. OS-native speech ships as the installed default. Kokoro and Pocket TTS
can be presented in Marketplace, but remain unavailable until their model-pack
runtime and verified download adapter are implemented in C#.
