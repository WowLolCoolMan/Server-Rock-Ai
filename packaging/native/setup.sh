#!/usr/bin/env bash
set -euo pipefail

package_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
install_root="${HOME}/.local/opt/server-rock-ai"
launch=0

while [[ $# -gt 0 ]]; do
  case "$1" in
    --package-root) package_root="$(cd "$2" && pwd)"; shift 2 ;;
    --install-root) install_root="$(mkdir -p "$2" && cd "$2" && pwd)"; shift 2 ;;
    --launch) launch=1; shift ;;
    -h|--help) echo "Usage: setup.sh [--package-root PATH] [--install-root PATH] [--launch]"; exit 0 ;;
    *) echo "Unknown option: $1" >&2; exit 2 ;;
  esac
done

manifest="${package_root}/release-manifest.json"
if [[ ! -f "$manifest" ]]; then echo "release-manifest.json is missing from ${package_root}" >&2; exit 1; fi
version="$(sed -n 's/.*"version"[[:space:]]*:[[:space:]]*"\([0-9][0-9A-Za-z.+-]*\)".*/\1/p' "$manifest" | head -n 1)"
if [[ -z "$version" ]]; then echo "The release manifest has no valid version." >&2; exit 1; fi

version_root="${install_root}/${version}"
current="${install_root}/current"
previous="${install_root}/previous"
if [[ -e "$version_root" ]]; then echo "Version ${version} is already installed at ${version_root}" >&2; exit 1; fi
mkdir -p "$install_root"
mkdir -p "$version_root"
cp -a "${package_root}/." "$version_root/"
if [[ -e "$current" || -L "$current" ]]; then rm -rf "$previous"; mv "$current" "$previous"; fi
ln -s "$version_root" "$current"

mkdir -p "${HOME}/.local/bin" "${HOME}/.local/share/applications"
cat > "${HOME}/.local/bin/server-rock-ai" <<EOF
#!/usr/bin/env bash
exec "${current}/ServerRock.Native" "\$@"
EOF
chmod +x "${HOME}/.local/bin/server-rock-ai" "${current}/ServerRock.Native"
cat > "${HOME}/.local/share/applications/server-rock-ai.desktop" <<EOF
[Desktop Entry]
Type=Application
Name=Server Rock AI
Comment=Local-first AI workspace
Exec=${HOME}/.local/bin/server-rock-ai
Icon=applications-development
Terminal=false
Categories=Development;Utility;
StartupNotify=true
EOF

echo "Installed Server Rock AI ${version}."
echo "Launch from your desktop menu or run: server-rock-ai"
if [[ "$launch" -eq 1 ]]; then exec "${current}/ServerRock.Native"; fi
