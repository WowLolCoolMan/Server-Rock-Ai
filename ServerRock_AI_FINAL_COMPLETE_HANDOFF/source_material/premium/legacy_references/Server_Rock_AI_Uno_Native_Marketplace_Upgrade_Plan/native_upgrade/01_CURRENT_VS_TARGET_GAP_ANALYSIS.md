# Why the current marketplace cannot meet the desired behavior

| Feature | Observed current state | Why insufficient | Required evolution |
|---|---|---|---|
| Catalog | `SkillCatalogService.BuiltIns`: 3 hard-coded definitions | Cannot discover arbitrary GitHub skill packages or manage versions | Versioned catalog metadata + installed package registry |
| Skill execution | Skills dialog sets `PromptBox.Text = skill.Prompt` | Manual prompt insertion is not autonomous skill routing; no lifecycle | Runtime skill resolver, context loader, tool runner and auditable activation |
| Provider compatibility | `SkillDefinition` has no provider/model/capability constraints | Claude-only skill could be offered to an incompatible model | Manifest allow/deny + capability requirements, deterministic enforcement |
| Installation | Plugin directory labels non-built-ins unavailable | Displaying entries does not install code, files, tools or dependencies | Staged package manager and health verification |
| Updates | App release updater exists | App releases do not track per-skill GitHub subpaths and commits | Per-install source refs, version pinning, change checks and rollback |
| Uninstall | App-wide native uninstaller | Cannot safely remove optional components while preserving shared dependencies | Ownership ledger, reference counts, scoped environments |
| MCP | Static integration entries | No proof of MCP process discovery/launch/permissions/tool schemas | Independent MCP runtime lifecycle and tool registry |
| UI | C# `ContentDialog` listings | Not sufficient for hundreds of packages, install queues, dependency detail, diagnostics | Dedicated native Marketplace and My Skills workspaces |
| Chat history | `conversations.json` + native import/export | Not equivalent to a user-browsable per-conversation Chat History folder | Export/sync view and migration without replacing canonical store blindly |
| Release | Documented outstanding packaged-artifact, signing, accessibility checks | Source code and unit tests alone do not prove desktop reliability | Windows/Linux packaged E2E evidence |

Severity: architecture/lifecycle and permission enforcement P0; native usability, GitHub updates, history and release evidence P1; catalog expansion and aesthetics P2. Priorities are recommendations, not assertions of existing issue tracker labels.
