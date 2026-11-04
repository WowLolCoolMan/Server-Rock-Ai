// Decides whether this build can install updates in place.
//
// Some builds can't: unsigned macOS apps (macOS rejects unsigned updates), the
// Windows portable .exe, and the Linux tar.gz. The Linux and portable checks
// mirror electron-updater's own platform detection (the APPIMAGE environment
// variable and the resources/package-type file written for deb/rpm builds).
// For these builds the app says the update must be downloaded instead of
// letting the updater fail with a 404 or an installer error.
const path = require("node:path");

function manualUpdateReason({ platform, env, resourcesPath, fileExists }) {
  const download = " Download the new version from GitHub Releases instead.";
  if (platform === "darwin")
    return "This macOS build isn't code-signed, so it can't update itself." + download;
  if (platform === "win32" && env.PORTABLE_EXECUTABLE_DIR)
    return "The portable app can't update itself." + download;
  if (
    platform === "linux" &&
    !env.APPIMAGE &&
    !fileExists(path.join(resourcesPath || "", "package-type"))
  )
    return "This Linux package (tar.gz) can't update itself." + download;
  return "";
}

module.exports = { manualUpdateReason };
