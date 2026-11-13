# Implementation evidence ledger — distinguish artifacts from shipping app

## Earlier source/document review (historical, revalidate at current HEAD)
| Area | Previously observed in Server Rock AI | Not established |
|---|---|---|
| Platform | Uno/.NET 10 native desktop projects | Packaged release verified on both OSes |
| Chat | Local/cloud endpoints, streaming, conversation persistence | Full user-facing Chat History folder, robust migration |
| Coding | File editing, GitHub and terminal workflows, bounded context | Autonomous reliable typed tool loop, parallel agent execution |
| Models | Model library and active provider selection | Seamless consent-aware multi-provider routing and failover |
| Skills | Three static prompt templates in SkillCatalogService | Real install/enable/update/remove marketplace |
| Plugins/MCP | Static integration catalog | Managed MCP install/permission/update/uninstall lifecycle |
| Release | Native scripts and documentation exist | Signed installer, upgrade/rollback, clean packaged smoke verified |

Earlier reports also raised: Windows CI failures, hardcoded marketplace, bounded coding context, placeholder scheduler, Git staging risks and installer issues. These are **prior reported findings**, not confirmed defects at current HEAD. Verify before assigning.

## Separate prototype/deliverable evidence
- A **Codex Hybrid Router** was produced as a separate ZIP/plugin/CLI companion, not proven integrated into Server Rock AI.
- Its README/verification previously reported 38/38 simulated/unit tests for v0.3.0 and limited black-box testing for an earlier version. Those do **not** prove real Windows, authenticated Codex Desktop, Ollama GPU, or in-app parallel agent functionality.
- Prior marketplace ZIPs and cross-platform installer prototypes were **specifications/prototypes**, not verified shipping Server Rock features.
- A 250-entry catalog in a ZIP is not proof of 250 audited, compatible, installable SKILL.md packages.

## Agent instruction
Classify every item: `VERIFIED_CURRENT`, `VERIFIED_HISTORICAL`, `REPORTED_HISTORICAL`, `SPEC_ONLY`, `SEPARATE_ARTIFACT`, `UNKNOWN`. Never promote a historical report into current completion without source and runtime evidence.
