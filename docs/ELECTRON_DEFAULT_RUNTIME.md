# Electron default runtime

Electron + React + the persistent .NET agent host are the default desktop
runtime for this repository. The Uno/XAML application remains available only
through explicitly named legacy commands while parity and retirement evidence
are completed.

## Commands

```text
npm run start          # Electron development runtime
npm run build          # Electron renderer production build
npm run open           # Open the built Electron runtime
npm run package:electron        # Stage a portable Electron + Agent Host artifact
npm run smoke:electron:artifact # Verify the staged artifact renders its core shell

npm run start:legacy-native
npm run build:native
npm run open:legacy-native
```

The default path keeps the renderer sandboxed and communicates with the .NET
host through the context-isolated preload bridge. The legacy commands are a
rollback and comparison surface; they are not the product direction.

The portable artifact bundles a self-contained Agent Host under
`resources/agent-host`; it does not require a source checkout or a local .NET
installation to start. Signed installer delivery remains a separate release
gate.

## CI evidence

`.github/workflows/electron-ci.yml` installs the renderer dependencies,
typechecks and builds the Electron surface, builds the persistent host, and
runs the disposable protocol smoke. This is the minimum replacement-runtime
gate for every push and pull request.
