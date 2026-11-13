# Ideas worth taking—features only, not code or architecture

These are **product-level ideas** prompted by viewing the broader self-hosted AI workspace category, including Odysseus. They are independent requirements for Server Rock. No mapping to Odysseus files, no original source, no copied layouts and no integration.

| ID | User outcome | Independent Server Rock behavior | Verification |
|---|---|---|---|
| I-01 | Discoverable multi-workspace navigation | Expose Chat, Agent, Projects, Models, Skills, Images, Tasks and Settings in a consistent native shell; retain navigation history and drafts. | User can reach every workspace from the main rail without a modal and return without data loss. |
| I-02 | Tasks that continue while browsing | Make each accepted long-running job a durable Server Rock task with status, artifacts and resume rules. | Close any page and reopen Tasks; see same run and accurate status. No phantom success. |
| I-03 | Full skill lifecycle | Use Draft / Installed / Enabled / Needs review / Update available / Disabled states for locally installed skills. | Users can preview, test, enable, update and roll back skills independently. |
| I-04 | Useful skill recommendations | Index just skill summaries initially, and load full instructions only for matching user intent. | Agent states why a skill is suggested; no surprise activation or execution. |
| I-05 | Skill quality testing | Try a skill against a fixture task and display outcome, time and evidence; keep failed skills as drafts. | Tests show pass/fail/inconclusive and never auto-publish an unverified skill. |
| I-06 | Side-by-side model comparisons | Offer two or three chosen models for the same sanitized prompt with blinded results and timing. | Comparison labels and original text remain stable while user compares. |
| I-07 | Hardware-aware model discovery | Rank downloadable local models for device RAM/VRAM, runtime compatibility, disk and intended use. | Unknown sizes/capabilities are explicitly unknown rather than fabricated. |
| I-08 | Research with visible evidence | Support a bounded multi-source research task with source list, uncertainty and citation links. | Each factual claim in research is traceable to captured evidence. |
| I-09 | Persistent research artifacts | Store generated research notes and source snapshots under the originating task. | A report reopens intact after restart; source failures remain visible. |
| I-10 | Transparent tool activity | Show a per-task activity timeline for actions, inputs, outputs, permissions and test evidence. | No hidden write; each step identifies whether it actually executed. |
| I-11 | Independent notes and scratchpad | Provide user notes that can become agent context with opt-in explicit selection. | Notes do not silently enter cloud prompts or get modified by agent. |
| I-12 | Scheduled tasks users can inspect | Build task recurrence, next-run previews and opt-in notifications for local agent jobs. | Every schedule can be paused, edited, cancelled and reviewed. |
| I-13 | Page-specific inspectors | Open file diffs, references, artifacts, model details and skill information in resizable right panels that reserve real width. | Inspectors never float over the composer or block the page. |
| I-14 | Command palette | Search navigation destinations and safe actions with keyboard shortcuts. | Escape closes palette; focus returns to correct source control. |
| I-15 | Workspace restoration | Remember selected project, active page, open task and panel sizes safely. | After restart restores meaningful context without executing side effects. |
| I-16 | Document workbench | Offer preview, edit, save-as, diff and export for agent-generated text artifacts. | Dirty indicator and unsaved-change confirmation prevent data loss. |
| I-17 | Media job queue | Expose queued/running/failed/completed image generations and their workflow dependencies. | Switch pages without cancelling generation; cancellation targets exact job. |
| I-18 | Integration health center | Summarize models, ComfyUI, Git, GitHub and optional tools with connected/degraded/offline states. | Each service has one clear actionable diagnosis and retry. |
| I-19 | Permission-centric automation | Show the exact files, commands, websites and credentials a run will touch before granting action rights. | Approval cannot expand to unrelated tools and expires appropriately. |
| I-20 | Device and privacy indicators | Show when computation is local, when prompts leave the device and estimated cloud spend. | Cloud fallback never silently transmits private workspace material. |
| I-21 | First-run setup checklist | Guide users through connect runtime, choose project, choose model and test generation, skipping already satisfied steps. | A new user has working chat path without reading the README. |
| I-22 | Recovery center | Explain failed operations with copyable diagnostics, retry and revert choices. | Errors persist in activity history rather than disappearing behind the current page. |
| I-23 | Unified updates center | Show skill/plugin/model/app updates separately with explicit provenance and change summaries. | User can defer, accept or rollback supported updates. |
| I-24 | Accessible minimized side panels | When window narrows, replace inspector with a tab or drawer and preserve keyboard focus. | No user control becomes covered, clipped, unreachable or hidden behind overlays. |
| I-25 | Clean, restrained original visual identity | Use Server Rock typography, meaningful task badges, neutral surfaces, one accent, and information density settings. | Dark/light/high-contrast screenshots read as one coherent Server Rock app. |
| I-26 | Own project and workflow templates | Offer first-party guided workflows (fix build, document project, audit UI, prepare release) independently of third-party skill packs. | Each workflow contains explicit preview, progress, stop and verifiable completion. |

## Prioritize by need

**First:** I-01, I-13, I-24, I-25 for the immediate UI problems; I-10, I-18, I-22 for truthful status. **Next:** I-02, I-03, I-04, I-19, I-20 for agent/product trust. **Then:** model comparisons, research, notes, documents, schedules and expanded workflows as separately tested features.

### What not to imitate

Do not assume another app is correct. Avoid giant modal stacks, aggressive multitasking that makes actions hard to follow, background jobs without durable owners, context-heavy tool menus that overload small models, and shell access governed only by prompt instructions. Implement the simplest native Server Rock UX that meets the behavior.
