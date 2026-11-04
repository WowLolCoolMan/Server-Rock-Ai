# App updates through GitHub Releases

The app checks its public GitHub Releases feed directly. The check does not require a Server Rock account or a service operated by the developer. Stable and beta tags use `vMAJOR.MINOR.PATCH` and `vMAJOR.MINOR.PATCH-beta.N` respectively.

The update screen selects the newest matching published release, locates a compatible installer, and displays the SHA-256 digest supplied by GitHub’s release asset API. It links to that exact release page so users can review the release notes and download the installer themselves. Drafts, malformed tags, unsupported operating systems, missing digests, and unexpected GitHub URLs are ignored. If no release exists, the screen shows that state and keeps the download action disabled.

Checks are manual. This implementation does not silently download, install, replace, or roll back the app. It trusts GitHub’s published asset digest and directs the user to GitHub; independent publisher signatures and automatic installation remain future work. Windows ARM64 installers and formats beyond Windows x64 NSIS, macOS DMG, and Linux AppImage are not currently selected by the updater.

To publish an update, create a version tag and use the repository release workflow to build the supported artifacts and attach them to a GitHub release. Keep release tags immutable after publication so the displayed version and asset digests remain meaningful.
