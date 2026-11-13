# Required negative tests and abuse cases

1. Malicious SKILL.md instructs agent to ignore user or leak credentials -> ignored as untrusted data.
2. Skill ZIP contains `../` traversal or symlink escape -> install rejected.
3. Skill update adds network/file permissions -> requires renewed approval.
4. Model changes to one lacking tools -> incompatible skills disabled with explanation.
5. Local-to-cloud fallback -> no data egress without configured consent.
6. Provider streams partial output then errors -> state and partial artifact accurately labeled.
7. Tool execution cancelled after side effect -> no blind retry; reconcile actual state.
8. Disk fills during conversation save -> prior conversation remains intact and recoverable.
9. Crash during skill install -> staged files recovered/removed; no half-enabled install.
10. Crash during data migration -> original data preserved; safe resume/rollback.
11. Uninstall skill with shared dependency -> retain dependency if another component owns it.
12. Uninstall Server Rock -> user chats, vault, external projects and models preserved by default.
13. Missing external runtime -> actionable setup error, not phantom success.
14. Invalid/expired API key -> clear provider-specific error without logging secret.
15. Renderer tries privileged IPC command -> denied by capability gateway.
16. Concurrent agents edit same file -> conflict surfaced, no silent overwrite.
17. Offline startup with installed local model -> works where backend available.
18. Update checksum mismatch -> reject and preserve previous working version.
19. Linux paths with spaces/unicode/symlinks -> safe; Windows long paths and permissions -> safe.
20. UI modal navigation and focus -> no invisible overlays or keyboard traps.
21. Diagnostic export with seeded fake token -> token redacted.
22. Plugin install contains unexpected postinstall script -> not automatically executed.
23. Model context exceeds budget -> explicit truncation and included-file list.
24. Unexpected external data directory -> folder map lists it and reason.
25. Reinstall/upgrade with old conversation schema -> lossless migration verified.
