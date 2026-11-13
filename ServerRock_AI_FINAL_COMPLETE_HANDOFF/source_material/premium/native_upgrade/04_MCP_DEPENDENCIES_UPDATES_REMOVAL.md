# MCP, updates and ownership

Separate MCP servers from prompt skills. MCP server package tracks executable/container/runtime, transport, command/args, environment secret references, permission scopes, tool schema, startup health, process ID/group and shutdown behavior. Never import third-party instructions as privileged app policy. Require explicit consent for network/filesystem access and secret delivery.

Ownership ledger records component ID, version, source, path, checksum, install transaction, created settings, process lifecycle, dependency references, cache and uninstall policy. Shared runtimes are reference-counted. Keep user-provided model weights and chat files by default.

GitHub source updates must consider tags/releases, tracked branch commits and subdirectory changes. Distinguish 'new commit upstream' from 'compatible update available'. Compare permission diffs and manifest schema before activation. Stage updates, test, preserve previous working version, and support rollback.

Tests: install → enable → invoke → disable → update → rollback → uninstall → reinstall; interrupted install, offline GitHub, rate limit, malicious path traversal, missing runtime, permission change, shared dependency, Windows process tree, Linux process group.
