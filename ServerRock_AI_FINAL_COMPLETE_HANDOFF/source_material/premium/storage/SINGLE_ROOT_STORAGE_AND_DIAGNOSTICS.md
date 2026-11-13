# Mandatory requirement: one discoverable Server Rock AI folder

## User intent
The user wants all app binaries, libraries, configuration, conversation logs, projects, installed skills, MCP metadata, model downloads where managed by Server Rock, diagnostic logs, and credential *references* discoverable under **one user-selected Server Rock AI root**. They want to inspect and debug the installed app without chasing files around the operating system.

## Recommended layout
ServerRockAI/
  app/
    current/                  # binaries, DLLs, assets, runtime dependencies
    versions/                 # optional versioned installs for rollback
  data/
    conversations/            # chats, exports, indexes, attachments
    projects/                 # app-managed project workspaces; external projects remain external by explicit choice
    skills/                   # manifests, installed skill payloads, lockfiles
    integrations/             # MCP and provider configs, no plaintext credentials
    models/                   # downloads managed by Server Rock, optional shared external runtimes
    settings/                 # human-readable JSON/TOML settings, versioned schemas
    credentials/              # encrypted vault files or secure-store references only
    logs/                     # user-readable diagnostics, sanitized and rotated
    backups/                  # explicit local backups
    cache/                    # disposable cached data
    state/                    # install/update ledger, operation journal, recovery
  tools/                      # optional bundled or user-approved managed runtimes
  README-FOLDERS.md           # file map and troubleshooting
  manifest.json              # app version, schema, owned paths, checksums where appropriate

## Critical design distinctions
- Do NOT store raw API keys as plaintext in easily browsable files. Make **where credentials are managed** easy to find: credential entries list provider, alias, last-updated date and secure storage type, with a reveal/copy action only after appropriate OS/user authentication if supported. Do not write secrets to logs, exports, ZIPs, or crash reports.
- A truly portable encrypted vault can use a user-supplied passphrase and authenticated encryption with a proven library and KDF; an OS keyring-backed vault may depend on OS-managed secrets outside the root. Document the tradeoff and never promise physical single-folder storage if OS facilities are involved.
- External model servers, OS runtimes, hypervisors, GPU drivers, shared caches, operating-system temp files, OS-managed secrets and app shortcuts may remain outside the root. Distinguish app-owned from external resources, and show links to external locations in the folder map. Avoid duplicating large user-managed model files.
- Do not move existing user data destructively. Offer opt-in migration with dry run, free-space check, integrity verification, rollback and source preservation until confirmed.
- Do not install to a privileged folder and then write mutable data under it without proper access control. Prefer a writable per-user root; support a managed installed mode and a portable folder mode with explicit limitations.
- Single root must not imply all data is automatically backed up or secure; include permissions, disk encryption recommendations, retention, and secure deletion limitations.
- Sensitive crash dumps and telemetry must be disabled by default or scrubbed and consent-controlled. Add log rotation and redact tokens, prompts, file paths when appropriate.
- Support Windows and Linux path handling, Unicode, symlinks, long paths, read-only media, multi-user accounts, portable drive disconnects, upgrade/rollback and uninstall.
- Keep core files visible and documented; do not claim that distributing readable files prevents reverse engineering. PDB/symbol packages or symbol server can make diagnostics easier while preserving normal release optimizations.

## User experience
- Settings > Storage & Diagnostics > **Open Server Rock Folder**
- **Open Chat Logs**, **Open Skills**, **Open Models**, **Open Logs**, **Open Credential Manager**, **Export Sanitized Diagnostics**, **Validate Storage**, **Move Data Folder**
- A read-only Folder Map listing paths, size, ownership, privacy category, migration state and external dependencies.
- A crash/recovery view with last operation, safe restart, diagnostics, issue-report export.
- Human-readable chat export (JSON + Markdown) with timestamps and model/provider metadata; warn if prompts include sensitive information.
- Include a `README-FOLDERS.md` generated with every installation describing each folder, backup guidance and redaction boundaries.

## Engineering cleanup
- Inventory existing data locations, hidden defaults, temp writes and platform-specific paths before changing them.
- Centralize all paths in `IAppPaths` and `IStorageLayoutService`; forbid arbitrary ad-hoc user data paths in UI and Core.
- Extract persistence, provider and installation logic from large MainPage partials into services/view models without changing behavior.
- Make schema migrations versioned, transactional and reversible where possible.
- Add a diagnostics facade with structured events, correlation IDs, redaction and configurable log levels.
- Add installer and portable mode tests covering all owned paths and confirming no unexpected app-owned writes elsewhere.
- Add startup integrity checks for app-managed files and recoverable errors for missing/corrupt settings.
- Include a test asserting that credentials and tokens never appear in diagnostics or exported chat metadata.

## Acceptance
1. A new user can find every Server Rock-owned runtime file and data category from the single root and Folder Map.
2. No app-owned persistent data is silently created outside the configured root, except documented unavoidable OS-managed integration artifacts.
3. Chat logs can be read/exported, searched and backed up without reverse engineering binary formats.
4. Credential storage is discoverable and manageable but secrets remain encrypted/protected.
5. Portable and installed modes behave predictably across Windows/Linux and upgrade/rollback.
6. Debug reports contain useful reproducible evidence without leaking secrets.
