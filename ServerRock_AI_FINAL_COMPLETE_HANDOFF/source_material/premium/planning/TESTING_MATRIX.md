# Cross-platform test matrix

| Layer | Windows | Linux | Gate |
|---|---|---|---|
| Core unit and contract | Required | Required | Per relevant change |
| Persistence and migrations | Required | Required | Before storage merge |
| Provider routing and consent | Required | Required | Before provider merge |
| Skills install/update/rollback | Required | Required | Before marketplace merge |
| MCP permissions and cancellation | Required | Required | Before integration merge |
| Native UI navigation and accessibility | Required | Required | Before UI merge |
| Packaged install/update/uninstall | Required | Required | Release gate |
| Crash recovery and sanitized export | Required | Required | Release gate |

Test fixtures: provider stubs, offline responses, sample conversations, safe dummy API keys, skill fixtures, fake MCP server, migration from legacy layouts, malicious archives and malformed configs.

Acceptance evidence: commands, exit codes, test names, screenshot or accessibility-tree captures, package identifiers, clean-install/upgrade results, regression list. Never report a test as passed unless executed.
