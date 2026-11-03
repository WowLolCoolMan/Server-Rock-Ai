# Rolling updates

Packaged Server Rock: AI builds use electron-updater with GitHub Releases as the update channel. The application checks only when the user selects Check for updates in Settings; there is no background telemetry or analytics service.

## Release workflow

1. Update the version in package.json.
2. Build the platform artifacts:
   - Windows: npm run build:win
   - Linux: npm run build:linux
   - macOS: npm run build:mac
3. Create a GitHub Release tagged with the exact version, for example v0.1.3.
4. Upload the generated installer artifacts and the generated update metadata files (latest.yml, latest-linux.yml, or latest-mac.yml plus its blockmap files).
5. Users can check for the new release in Settings. They choose when to download and when to restart and install it.

The GitHub repository is configured in package.json under build.publish. Development runs intentionally report that updates are available only in packaged builds.

Windows NSIS installs support in-app replacement. Portable Windows builds can detect a release but should use the downloaded installer for replacement because a running portable executable cannot safely replace itself.
