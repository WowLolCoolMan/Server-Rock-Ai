# Marketplace and installation lifecycle
## User experience
Discover > skill details (source, license, requirements, compatibility, permissions, version) > Install > My Skills > Enable/Disable > Pin/Unpin > Update > Remove. Search, categories, collections and packs. Show incompatible skills but explain why unavailable. Install does not imply enabled; enabled does not imply currently loaded.

## Manifest fields
id, display_name, description, category, skill_type (instructions|tool|mcp|pack), source_repo, source_subdirectory, source_ref, installed_commit_sha, release_version, integrity_hash, license, publisher, supported_platforms, provider_allowlist, model_allowlist, model_denylist, required_capabilities, required_tools, dependencies, permissions, entrypoint, install_method, update_channel, compatibility_test_results.

## Install/update process
Resolve exact upstream path and pinned ref; validate repository and manifest; show permissions and disk impact; stage in private directory; verify hashes and compatibility; install dependencies in scoped environments; run health checks; atomically activate; write ownership ledger. Update checks use GitHub releases/tags or relevant subtree commits with caching, rate-limit handling, offline behavior and ETags where possible. 'Update all' processes independent components and reports individual failures. Require renewed approval for increased permissions. Preserve prior version for rollback.

## Clean removal
Stop tasks/processes; unregister tools; remove app-owned files and exclusive dependencies; preserve shared dependencies; ask separately about user data; remove app-created startup entries and caches; verify; show residue and recovery options. Maintain transaction journal, dependency graph, ownership ledger and reference counts. Never delete external user files based on guessed paths. Include install→update→uninstall→reinstall tests on Windows and Linux.

## Catalog scale
Start with verified curated entries and expand toward 250–500. Remote catalog metadata should not require downloading every repository. Clearly label research/reference repos versus installable packages.
