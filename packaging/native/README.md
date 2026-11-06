# Native Windows packaging

The release archive contains a versioned-copy installer, an explicit uninstaller,
and a rollback script. Installation keeps application data outside the install
root (`%LOCALAPPDATA%\ServerRock\AI`), moves the prior version to `previous`,
and switches `current` atomically enough for a user-initiated upgrade. Rollback
only swaps those versioned directories; it never deletes chat history, settings,
projects, or credentials.

These scripts are a transitional packaging layer until a signed MSIX/DMG/DEB
channel is provisioned. Tagged releases still require the repository signing
gates and real certificate/key material; unsigned artifacts cannot publish.
