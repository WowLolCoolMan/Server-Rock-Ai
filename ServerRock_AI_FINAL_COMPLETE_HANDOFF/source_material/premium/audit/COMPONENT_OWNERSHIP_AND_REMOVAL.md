# Optional component lifecycle and ownership

Every optional install gets stable component ID, pinned source, version, hash, exact owned paths, shared dependency references, configuration entries, subprocess ownership, service registration and user-data classification.

Install: preview -> validate -> stage -> verify -> activate -> persist ledger -> smoke. Update: compare -> show diff/permissions -> stage -> verify -> switch atomically where possible -> rollback on failure. Remove: stop owned processes -> disable -> reverse owned registration -> remove exclusively owned files -> decrement shared refs -> preserve user data and external dependencies -> verify no orphaned services or app-owned background processes.

User-visible uninstall report distinguishes removed, retained (user data), retained (shared dependency), external, and failed. Never delete unrelated project folders, API credentials, system dependencies or model downloads by default.
