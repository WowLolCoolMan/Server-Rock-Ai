# IDE-style workflows and UI behaviors

## Global layout
Navigation: Home, Chat, Projects, Agents, Models, Skills, Integrations, Activity, Settings. Workspace with resizable editor/chat/terminal/activity panes; responsive min sizes; keyboard command palette; remembered layout per workspace; no inaccessible hidden layers.

## Coding task
Choose workspace -> inspect context/permissions -> create plan -> review proposed edits -> approve according to risk -> execute in isolated worktree -> show terminal and diff -> run tests -> evidence-backed completion -> merge/recover. All actions reflected in unified execution timeline with model/provider, files, commands, approvals and results.

## Chat and model switching
Show current route, provider, privacy boundary, tool/vision capability, context inclusion and token budget. Revalidate skills on model switch; never silently route local data to cloud. Preserve conversation branches and human-readable exports.

## Marketplace
Discover vs Installed vs Enabled are separate states. Inspect source/license/permissions/dependencies -> stage -> validate -> install atomically -> enable -> task-select after deterministic eligibility -> update diff -> rollback/remove. Keep user-owned files and unrelated dependencies.

## Integrated editor and terminal
Monaco editor for syntax/diff if React shell selected; equivalent reliable component if Uno retained. PTY sessions with explicit workspace and process ownership, visible command history, exit status, resize, copy/paste, encoding and safe termination. A terminal is not a privilege bypass.

## Transparency
Activity inbox, diagnostics viewer, open-folder shortcuts, operation replay, clear success/failure, task budget and source provenance. Display summaries of actions, not purported private chain-of-thought.
