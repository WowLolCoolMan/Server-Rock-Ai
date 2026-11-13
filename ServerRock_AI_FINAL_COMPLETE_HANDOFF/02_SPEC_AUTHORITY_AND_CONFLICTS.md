# Canonical authority and contradiction register

This document is part of the final handoff and resolves contradictory instructions spread across the four historical ZIPs. Use **current repo reality**, not theoretical plans, to select implementations.

| Topic | Canonical decision | Historical ideas treated only as references |
|---|---|---|
| Desktop stack | **Keep existing native Uno/XAML/C#/.NET10** until owner explicitly chooses otherwise | Electron, Tauri, React, shadcn, browser-only UI components |
| Existing code | Refactor incrementally, preserve features/data/tests and release behavior | Total rewrite or 'start from scratch' directives |
| Native UI styling | `App.xaml`, `UiTheme.cs`, `DESIGN.md` tokens; add shared theme dictionaries/styles | Duplicated inline styling, unrelated CSS snippets |
| Backend | Extend `SkaiDesktop.Core` with services/adapters, DI, MVVM/state machines; OS-specific wrappers | Treating current MainPage as a backend or assuming a Node runtime ships |
| Product relationship | Independent Server Rock: AI desktop app | Unrequested coupling to Server Rock web/Core services, cloud entitlement requirement |
| Odysseus | **Study interaction/architecture ideas only; do not incorporate its code/assets** | Instructions that could imply importing the implementation |
| Marketplace | Opt-in, provenance-aware candidate registry with safe install/review/update/rollback | Blind `git clone` + run, claiming all 250 catalog entries tested |
| Security | Explicit scoped access, bounded tool execution, reviewed diffs, real platform isolation | UAC/admin shell by default, 'no hard blockers', pretend sandboxing |
| AI engines | Host/provider adapters selected for capabilities; honest limitations; isolated parallel runs | Assume one API shape handles every provider or every HF model |
| Data & history | Persistent local history with migrations, backups and strong rollback | Overwriting JSON profiles or silently deleting old sessions |
| Releases | Windows/Linux packages must pass signed install/upgrade/uninstall/rollback/runtime tests | Calling a CI compile or portable ZIP a verified production installer |
| Mockups vs product | Screens and demos are reference designs; code must use actual services and actual results | Reporting conceptual screenshots as shipped features |
| Timeline | Dependency / readiness gates without development-time estimates | Old rough engineering schedules |

**Precedence order:** current user requests and approved decisions > actual fresh checkout / repo contributor guidance > this final root's canonical documents > `source_material/premium/upgrade` > `source_material/independent` > `source_material/odysseus_ideas_only` > `source_material/skill_marketplace_250` > older material inside premium source. Never interpret historical example code as an unreviewed license to overwrite or execute.

**When source packs disagree:** record the exact file/paragraph, current code evidence, proposed resolution and whether owner approval is required. Never simply follow the most detailed or most forceful older instruction. Third-party repos are research sources only until licensing and provenance checks pass.

**Feature truth:** API exists ≠ working integration; compiled ≠ packaged; one OS smoke ≠ cross-platform; item appears in marketplace ≠ audited skill; mock UI ≠ implemented functionality. Use the evidence rules in `10_TEST_AND_RELEASE_ACCEPTANCE.md`.
