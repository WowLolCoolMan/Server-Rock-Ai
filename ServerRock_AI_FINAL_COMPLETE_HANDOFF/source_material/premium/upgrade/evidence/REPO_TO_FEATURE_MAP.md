# Implementation anchors: where the AI should start looking

| Topic | Existing source / entry point | Work to inspect before modifying |
|---|---|---|
| Shell/navigation | `native/ServerRock.Native/MainPage.xaml`, `SidebarView.xaml`, `MainPage.Navigation.cs`, `MainPage.Layout.cs` | Route state, popover ownership, back/forward, narrow layout, focus |
| Theme/styles | `native/ServerRock.Native/App.xaml`, `UiTheme.cs`, `DESIGN.md` | Central tokens, dark theme, typography, hover/focus, Linux fallback |
| Chat/stream | `MainPage.Chat.cs`, `MainPage.Messages.cs`, `MainPage.Branches.cs`, `OpenAiCompatibleProvider.cs` | Multiple simultaneous sessions, cancellation, streaming, async UI updates |
| Composer/model switch | `MainPage.Composer.cs`, `MainPage.ModelLibrary.cs`, `MainPage.SlashCommands.cs` | Capability labels, routing, per-session selection, dropdown placement |
| Project/workspace | `MainPage.Projects.cs`, `MainPage.CodingAgent.cs`, `LocalProject.cs`, `ProjectStore.cs` | File tree, ownership, context consent, worktrees and recovery |
| Code edits & undo | `MainPage.CodingAgent.Proposals.cs`, `WorkspaceFileEditor.cs`, `WorkspaceUndoStore.cs`, `WorkspaceTextDiff.cs` | Review before write, conflicts, resume, rollback, bulk actions |
| Terminal/process | `MainPage.Terminal.cs`, `WorkspaceTerminalSession.cs`, `WorkspaceCommandRunner.cs` | Process supervision, PTY, command policy, platform exit semantics |
| Model library | `ModelLibraryService.cs`, `AppSettings.cs` | Local discovery, downloads, license, compatibility, measured performance |
| Skills | `MainPage.PluginsAndSkills.cs`, `SkillCatalogService.cs` | Replace/static-catalog bridge with opt-in registry/install/update/remove |
| Integrations | `PluginCatalogService.cs`, `MainPage.GitHub*.cs`, GitHub services | MCP registry, scoped permission grants, managed connections |
| Persistent user data | `ConversationStore.cs`, `JsonSettingsStore.cs`, `ProjectStore.cs`, `ConversationArchive.cs` | Schema, backup, history search, atomic migration, single-root inventory |
| Secrets | `GitHubCredentialStore.cs` | Windows and Linux vault handling; hosted-provider support |
| Packaging & CI | `.github/workflows/native-*.yml`, `packaging/native`, `docs/NATIVE_RELEASE_ACCEPTANCE.md` | Signed artifacts, actual installers, rollback, clean-machine evidence |
| Tests | `native/SkaiDesktop.Core.Tests/*`, `scripts/audit-native-ui.cjs` | True runtime/UI tests, keyboard checks, negative and recovery tests |

**Do not assume a file exists or remains in this location in a newer checkout.** Always resolve actual symbols and code before editing.

## Refactor order
1. Stabilize navigation and design primitives with behavioral tests.
2. Add view-model/application service seams and typed operation contract.
3. Make operation journal and approval flow the source of task truth.
4. Add parallel agent orchestration, provider routing, MCP/skills and isolated worktrees by vertical slice.
5. Move persistence only after migration/backup tests; keep existing JSON readable until migration verified.
6. Production-quality installer, platform runtime, accessibility and release work before claims of availability.
