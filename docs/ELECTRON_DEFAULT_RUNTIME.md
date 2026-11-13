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

npm run start:legacy-native
npm run build:native
npm run open:legacy-native
```

The default path keeps the renderer sandboxed and communicates with the .NET
host through the context-isolated preload bridge. The legacy commands are a
rollback and comparison surface; they are not the product direction.

## CI evidence

`.github/workflows/electron-ci.yml` installs the renderer dependencies,
typechecks and builds the Electron surface, builds the persistent host, and
runs the disposable protocol smoke. This is the minimum replacement-runtime
gate for every push and pull request.
