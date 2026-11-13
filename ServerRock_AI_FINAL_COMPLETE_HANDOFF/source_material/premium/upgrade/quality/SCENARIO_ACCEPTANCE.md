# Acceptance scenarios — run as interactive end-to-end tests

## First-run and navigation
- Fresh profile: app launches into usable state, selects/detects local model; cloud remains optional. No broken default screen.
- `Ctrl+K` opens global command palette; navigate to Project/Skill/Settings via keyboard, Esc returns focus, Back restores route.
- Rapidly resize from 1440×900 to 760×560 during model picker and permissions dialog; controls stay usable, no underlying click-through or stuck overlay.
- Switch between conversations/projects repeatedly; drafts, scroll, selected model and pane sizes persist. Context is never silently sent to wrong project.

## Core chat
- Streaming response interrupted mid-token; Stop acknowledges cancellation and saves partial text without marking complete.
- Provider disconnect/rate limit/auth failure: user gets precise nonsecret message, retry or explicit local fallback; no duplicated tool side effects or hidden cloud request.
- Attach image/text/PDF then choose cloud; app shows selected content/size/provider scope. Temporary chats are not saved; archive import repeated is idempotent.

## Coding agent
- Open permitted project folder, inspect file, request edit, view diff, deny: **no file changed**. Approve: only authorized files changed, guarded undo works.
- Change file externally between proposal and approval: stale hash blocks and shows compare. Symlink outside root cannot be edited.
- Run test in terminal, cancel a spawned child tree, close/reopen window; no orphan process or falsely completed run.
- Start two write-capable agents; worktrees separate. Merging is explicit and handles conflict, user edits preserved.

## Models/skills/MCP
- Local-only profile with misconfigured remote URL refuses remote prompt; Auto fallback discloses provider and requests consent.
- Same model name on two hosts remains distinguishable. Ollama unavailable gives retry instructions and saved chat draft.
- Candidate skill install shows license/source/permissions, staged atomic install and rollback. Disabled skill never selected. Malicious archive/path traversal rejected.
- MCP server crashes/hangs: timeout/cancel, process cleanup, exact connection status, no automatic privileged restart.

## GitHub/activity/release
- GitHub link opens connection/browse in context, not Settings redirect; expired auth shows reauthorize; push requires reviewed destination and permission.
- Activity lists real run events/downloads/approvals with links to originating task; notification state persists across restart.
- Clean Windows/Linux install, upgrade, offline, corrupt download and rollback preserve user data. Unsigned/invalid update blocked.
- Export diagnostics previews redaction; no secret leaked. Linux missing keyring refuses plaintext save with instructions.

## Evidence output for each scenario
`case_id`, commit SHA, OS/distro/build, native artifact hash, screen dimensions/scaling, environment prerequisites, steps, expected, actual, pass/fail/blocked, screenshot/video/log, reviewer and outstanding issue. Test automations should create fixtures in disposable temporary workspaces only and must never mutate the user's real repo without approval.
