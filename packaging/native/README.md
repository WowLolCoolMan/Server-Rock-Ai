# Native desktop packaging

The release archive contains matching setup entry points for Windows and Linux, a versioned-copy installer, an explicit uninstaller,
and a rollback script. Installation keeps application data outside the install
root (`%LOCALAPPDATA%\ServerRock\AI`), moves the prior version to `previous`,
and switches `current` atomically enough for a user-initiated upgrade. Rollback
only swaps those versioned directories; it never deletes chat history, settings,
projects, or credentials.

These scripts are a transitional packaging layer until a signed MSIX/DMG/DEB
channel is provisioned. Tagged releases still require the repository signing
gates and real certificate/key material; unsigned artifacts cannot publish.

## Setup

Windows PowerShell:

```powershell
.\packaging\native\setup.ps1 -PackageRoot . -Launch
```

This installs under `%LOCALAPPDATA%\Programs\Server Rock AI`, creates a stable executable shim, and adds **Server Rock AI** to the Start Menu.

Linux:

```bash
chmod +x packaging/native/setup.sh
packaging/native/setup.sh --package-root . --launch
```

This installs under `~/.local/opt/server-rock-ai`, creates `~/.local/bin/server-rock-ai`, and adds a desktop-menu entry at `~/.local/share/applications/server-rock-ai.desktop`.
