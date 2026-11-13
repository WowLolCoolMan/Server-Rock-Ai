# UI information architecture

## Top-level navigation
Home (recent activity and continue), Chat, Projects, Agents, Models, Skills, Integrations, Activity, Settings.
Command palette can navigate and invoke safe, discoverable actions. All routes must be deep-linkable where appropriate and preserve back/forward state.

## Home
Resume previous work, current model/provider, pending approvals, recent projects, recent conversations, relevant alerts. No decorative empty cards.

## Chat
Conversation list/search/folders; clear active model and local/cloud badge; token/context usage when available; attachments; source/context inspector; streaming state; cancel; regenerate/branch; export; privacy preview before remote send. Error states must offer retry and diagnostics.

## Projects
Root folder, recent files, context inclusion, agent tasks, diff review, terminal execution permission and activity. External projects are allowed only by explicit user selection and shown as external.

## Agents
Task specification, plan, queued/running/waiting/failed/completed state, approval inbox, per-step tool outputs, diff preview, test results, recovery and run replay. Do not display internal chain-of-thought.

## Skills
Discover, inspect, trust signals, dependencies, compatibility, permission manifest, install/update/rollback/remove, auto-routing explanation, conflict handling and update indicators.

## Models
Local vs cloud, installed vs available, provider health, memory/disk implications, download progress, model capabilities, active route and fallback consent.

## Integrations
MCP connections, credentials, scopes, health, tools exposed, disable/revoke, logs and provenance.

## Settings
General, Appearance, Models & Providers, Security & Privacy, Storage & Diagnostics, Integrations, Updates, About. Provide Open Server Rock Folder and Folder Map.

## Interaction invariants
No hidden dialogs behind overlays; only one modal decision focus at a time. No action button without a real handler. Explicit loading, empty, offline, permission-denied and failure states. Preserve unsaved edits and warn on navigation. Keyboard and screen reader testing.
