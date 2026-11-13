# Windows and Linux: GitHub Skill Marketplace Setup

This ZIP **does not vendor 250 third-party skills**. A Server Rock desktop user installs an individual skill from GitHub when desired. The CLI demonstrates the download and update protocol; the shipping product should implement equivalent operations in .NET.

## Windows 11 PowerShell

Install [Git for Windows](https://git-scm.com/download/win) and [Python 3](https://www.python.org/downloads/windows/) (Python is for prototype only, not required by final native application). Open PowerShell in this extracted ZIP:

```powershell
git --version
py -3 --version
py -3 marketplace.py catalog --query dependency
py -3 marketplace.py install 003 --approve
py -3 marketplace.py status
py -3 marketplace.py check-updates
py -3 marketplace.py update 003 --approve
py -3 -m unittest discover -s tests -v
```

Storage: `%LOCALAPPDATA%\ServerRock\AI\AgentSkills\`. A local update leaves a `003.previous` backup until a human or application-controlled cleanup. The installed skill is initially **unreviewed**, not enabled to execute scripts.

## Linux (Ubuntu/Debian)

```bash
sudo apt-get update
sudo apt-get install -y git python3
python3 marketplace.py catalog --query comfy
python3 marketplace.py install 250 --approve
python3 marketplace.py status
python3 marketplace.py check-updates
python3 marketplace.py update 250 --approve
python3 -m unittest discover -s tests -v
```

Fedora: `sudo dnf install git python3`. Arch: `sudo pacman -S git python`. Storage: `${XDG_DATA_HOME:-$HOME/.local/share}/server-rock-ai/agent-skills/`. On both OSes, use `SERVERROCK_SKILL_HOME` to override in tests (not recommended for production). A network connection is required only for install and check/update; catalog and installed files work offline.

## Marketplace action flows

- **Discover:** only metadata stored locally in `catalog_250.json`; search does not download code.
- **Preview install:** fetch limited metadata or lightweight SKILL.md for display; show license, permissions and dependency requirements.
- **Install:** require explicit user approval, fetch GitHub HTTPS repo to disposable staging, materialize only chosen skill folder, reject symlinks and unreasonable size, verify `SKILL.md`, atomically move into private user data and record source tree SHA.
- **Check updates:** only on user action (or separately opted-in schedule), compare exact installed subtree SHA to remote subtree SHA; show update badge only where changed. Network/error statuses are not “Current.”
- **Update:** show what changed and permissions/license diffs, require approval, preserve modified local files and backup prior tree for rollback.
- **Enable:** after security/compatibility review. **Execution:** only via permission-bound tool APIs; never run scripts during installation or metadata scanning.

## App UX events

`Marketplace.SearchRequested`, `InstallPreviewRequested`, `InstallApproved`, `InstallProgress`, `SkillInstalledUnreviewed`, `SkillEnabled`, `UpdateCheckRequested`, `UpdateAvailable`, `UpdatePreviewRequested`, `UpdateApproved`, `UpdateApplied`, `UpdateFailed`, `RollbackAvailable`, `SkillDisabled`. These are proposed .NET event names, not existing APIs.

## Update badge JSON example

```json
{
  "id": "example.owner.skill",
  "status": "update-available",
  "installed_tree_sha": "sha-of-current-folder",
  "remote_tree_sha": "sha-of-changed-folder",
  "locally_modified": false
}
```

Never upload the user's private workspace files to GitHub during a public skill update check; only installed source repo/path and public revision metadata are necessary. Rate-limit and cache calls, and display when the last check occurred.
