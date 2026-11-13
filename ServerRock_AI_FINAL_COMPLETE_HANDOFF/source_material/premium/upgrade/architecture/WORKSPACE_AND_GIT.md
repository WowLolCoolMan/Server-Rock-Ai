# Workspaces, code operations, Git isolation and terminal

## Workspace identity
Keep user-owned project roots external to app data; store selected roots as normalized canonical paths with explicit trust and permission scopes. Do not silently walk symlinks outside root, hidden secrets, ignored build folders or device files. Separate read/context access from write/execute/push privileges.

## File editing
- Snapshot baseline hash, encoding/newline and permission bits; build diff before writing.
- Validate proposed path is inside authorized root after canonicalization, and is not a reparse-point/symlink escape on Windows/Linux.
- Reject binary/oversized/secret files unless explicit context and policies allow; do not quote whole repo into prompt automatically.
- Atomic stage/write and guarded undo; reread/hash before apply. Stale proposal requires compare and reapproval.
- Multiple file operations should use transaction-like manifest + rollback records; if partial apply, show exactly which files changed and recovery instructions.

## Git worktrees
Each write-enabled parallel agent gets a task-labeled branch/worktree from a known base commit. Tool execution CWD must bind to that worktree. Show branch, worktree path, assigned run and dirty state. On completion user reviews patch/tests and chooses cherry-pick/merge/manual compare. Avoid automatic cleanup of unmerged or dirty worktrees; never prune/delete worktrees without scoped consent and recovery evidence.

## Terminal
Use current Porta.Pty/XTerm.NET capabilities after verification; abstract differences via `ITerminalService`. Display trusted CWD, environment origin, running process and permitted scope. Agent commands use proposal/approval flow; user-typed terminal has different trust model. Start child process with nonsecret minimal env, bind/limit IO and terminate **process tree**, not only parent, on cancel. Shell selection differs between Windows PowerShell/ConPTY and Linux Bash/Zsh/PTY; quoting/escapes must be platform specific, no interpolated untrusted shell strings.

## GitHub operations
Credential via platform keyring, never console args/logs. Clone, browse and push require explicit repository and auth. Stage/commit/push separated; show actual remote/destination branch, diff and secrets guard. Handle nonfastforward and auth revocation without data loss. Repositories opener must present browse/sign-in flow in context, not send unexpectedly to Settings.

## Testing
Race external file changes against apply; symlink/path traversal; Unicode paths, case sensitivity, long paths and line endings; dirty worktree; spawn child process then cancel; login expires mid-push; git absent; network disconnected; terminal resize; 2 agents same file; reviewed partial write; rollback interruption.
