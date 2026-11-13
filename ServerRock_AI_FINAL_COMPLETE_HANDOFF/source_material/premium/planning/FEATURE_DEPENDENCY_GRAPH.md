# Feature dependency graph

```mermaid
flowchart TD
 A[Baseline and evidence] --> B[Core service boundaries]
 B --> C[Operation journal and approvals]
 B --> D[Storage paths and migration]
 B --> E[Design tokens and navigation]
 C --> F[Agent execution and progress]
 C --> G[Skill install lifecycle]
 D --> H[Conversation history and diagnostics]
 D --> G
 E --> I[Chat and model UI]
 E --> J[Marketplace UI]
 F --> K[MCP and tool orchestration]
 G --> J
 G --> K
 I --> L[Cross-platform integration tests]
 J --> L
 K --> L
 H --> L
 L --> M[Packaging and release validation]
```

## Gate definitions
- Before moving storage: inventory and backup, migration fixtures and rollback tests.
- Before auto-selecting skills: stable manifests, permissions, provenance and safe installer.
- Before parallel UI implementation: shared view models and navigation contracts.
- Before releasing: end-to-end workflows on Windows and Linux, upgrade/uninstall and credential redaction verified.
- Independent branches may proceed in parallel only where contracts and file ownership are nonoverlapping.
