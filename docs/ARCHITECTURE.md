# Architecture

Skai Desktop is a single native desktop app written in C# with the
[Uno Platform](https://platform.uno/) (WinUI/XAML API). Everything that ships
lives under `native/`. The repository's Node.js scripts are only release and
hygiene helpers; they are not part of the app.

This page is a map for people reading the code. For product scope, limits, and
release gates, see `docs/NATIVE_MIGRATION.md` and
`docs/NATIVE_RELEASE_ACCEPTANCE.md`.

## Projects

`native/ServerRock.Native.sln` contains three projects:

| Project | Type | What it is |
| --- | --- | --- |
| `ServerRock.Native` | Uno single-project app (`Uno.Sdk`) | The desktop UI: window, XAML page, dialogs, rendering. |
| `ServerRock.Core` | Plain `net10.0` class library | Everything that does not need UI controls: models, provider HTTP clients, local stores, workspace tools, GitHub, archive import. |
| `ServerRock.Core.Tests` | `net10.0` console app | A self-contained test runner for Core. No test framework: each check prints `PASS …` or throws. |

Dependency direction is one way: `ServerRock.Native → ServerRock.Core`, and
`ServerRock.Core.Tests → ServerRock.Core`. Core never references Uno or XAML,
which is why it can be tested with a plain console app.

Shared build settings:

- `native/Directory.Build.props` – nullable + implicit usings everywhere, central
  package management, a few suppressed SDK warnings, Release builds keep
  optimizations/trimming off (Uno 6.7 DevServer workaround, see comment there).
- `native/Directory.Packages.props` – every NuGet version (Markdig, PdfPig,
  Porta.Pty, XTerm.NET, Svg.Skia, …).
- `native/global.json` – pins the `Uno.Sdk` version.

## Platform targets

`ServerRock.Native.csproj` builds two target frameworks:

| Target | Runs on | Notes |
| --- | --- | --- |
| `net10.0-desktop` | Linux (X11 / framebuffer), Windows (Win32), macOS | Uno Skia renderer. Entry point: `Platforms/Desktop/Program.cs`. This is the target used for day-to-day builds. |
| `net10.0-windows10.0.26100` | Windows only | Windows App SDK (WinUI 3). Packaging manifest: `Package.appxmanifest`; publish profiles in `Properties/PublishProfiles/`. |

Windows-only code is wrapped in `#if WINDOWS` (voice input/output in
`MainPage.Voice.cs`, and window-handle setup for file/folder pickers in
`MainPage.Attachments.cs`, `MainPage.Companion.cs`, `MainPage.ImportExport.cs`, and
`MainPage.Projects.cs`). Everything else is shared.

Build and test:

```sh
dotnet build native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run   --project native/ServerRock.Native/ServerRock.Native.csproj -f net10.0-desktop
dotnet run   --project native/ServerRock.Core.Tests/ServerRock.Core.Tests.csproj
node scripts/audit-native-ui.cjs   # accessibility/focus "contracts" grepped from all MainPage.* files
```

## ServerRock.Native (UI)

Startup path: `Platforms/Desktop/Program.cs` (single-instance mutex, logging,
Uno host) → `App.xaml.cs` (creates the window and a `Frame`, global exception
logging via `App.Log`) → navigates to `MainPage`.

There is one page, `MainPage`. Its layout is in `MainPage.xaml`; nearly all
dialogs (Settings, Model Library, GitHub, project editor, terminal, review
dialogs…) are built in C# as `ContentDialog`s rather than in XAML.

`MainPage` is a `partial class` split into one file per feature. Every file
starts with a short comment saying what it does, what it depends on, and what
calls it. Member names are unchanged from before the split, so a search for a
method name still finds it.

| File | What it does |
| --- | --- |
| `MainPage.xaml` | Page shell: conversation area, composer, coding workspace bar, and companion overlay; the sidebar and header are dedicated controls. Owned by the UI/design work. |
| `MainPageHeader.xaml` / `.xaml.cs` | Extracted header control: navigation, route badges, branch controls, connection status, and new-chat action. Forwards semantic events and exposes bounded control accessors to MainPage. |
| `CodingWorkspaceBar.xaml` / `.xaml.cs` | Extracted Coding Agent workspace bar: project summary, folder context, GitHub, terminal, and undo actions. Forwards actions and the responsive size-change hook to MainPage. |
| `ConversationSurface.xaml` / `.xaml.cs` | Extracted conversation viewport: welcome state, connection setup, prompt suggestions, and message scroll surface. Forwards setup/suggestion actions and exposes the bounded message-view controls. |
| `ComposerToolbarView.xaml` / `.xaml.cs` | Extracted composer toolbar: model picker, tools disclosure, optional toggles, rewrite, image studio, and advanced actions. Forwards actions and the responsive size-change hook to MainPage. |
| `ComposerInputView.xaml` / `.xaml.cs` | Extracted composer input surface: status/queue banner, command hints, prompt editor, voice/send controls, and privacy note. Hosts `ComposerToolbarView` and forwards the existing composer actions to MainPage. |
| `SidebarView.xaml` / `.xaml.cs` | The left sidebar as its own control (from main): mode switch, primary actions, projects, folders, recents, pet controls. Forwards clicks as events and exposes named elements as `*Control` properties. |
| `MainPage.Sidebar.cs` | Connects MainPage to SidebarView (accessor properties such as `RecentList`, `ChatModeButton`) and the sidebar-only actions: Scheduled tasks, More, Update. |
| `MainPage.xaml.cs` | Shared page state (fields and Core services) plus compatibility accessors for extracted visual controls. |
| `MainPage.Infrastructure.cs` | Cross-cutting status classification/history dialog, guarded UI actions, and settings persistence shared by feature partials. |
| `MainPage.Lifecycle.cs` | Constructor, event wiring, timer setup, settings/history/project recovery, and initial rendering. |
| `MainPage.Layout.cs` | Responsive layout: sidebar drawer on narrow windows, two-row toolbars. |
| `MainPage.Navigation.cs` | Chat ⇄ Coding Agent switching, mode-dependent labels, keyboard shortcuts. |
| `MainPage.Connection.cs` | Model server connection check and Local/Remote status. |
| `MainPage.Chat.cs` | Sending a message (`SendClick`), streaming the reply, building the model request, Stop, prompt rewrite, speed measurements, friendly errors. |
| `MainPage.Messages.cs` | Message bubbles, Markdown replies, copy/edit/retry, web source cards, `RebuildConversationView`. |
| `MainPage.Branches.cs` | Conversation branches (‹ ›) and forks. |
| `MainPage.Composer.cs` | Composer toggles: Tools row, web search, temporary chat, project-files-in-chat, smart rewrite, edit-message banner, chat search. |
| `MainPage.Attachments.cs` | Attach picker, attachment limits, attachment chips, remote-upload consent key. |
| `MainPage.SlashCommands.cs` | `/` suggestion popup and command handlers, `/help`. The command list itself is `SlashCommandCatalog` in Core. |
| `MainPage.Voice.cs` | Voice input (Windows speech recognition) and spoken responses. |
| `MainPage.ModelLibrary.cs` | Model button and pinned-model hotswap menu, Model Library dialog, model selection, composer model label. |
| `MainPage.Settings.cs` | Settings dialog, Advanced (temperature) dialog, `ApplySavedCustomization`, dialog focus restore. |
| `MainPage.Recents.cs` | Saved-chat list, chat actions menu, rename/archive/delete, `SaveCurrentSession`. |
| `MainPage.ChatFolders.cs` | Sidebar chat folders. |
| `MainPage.ImportExport.cs` | Chat archive JSON/Markdown export and JSON archive import. |
| `MainPage.Projects.cs` | Project list, create/edit/remove, folder manager, `SaveProjects`. |
| `MainPage.CodingAgent.cs` | Coding Agent project file list, file editor, undo history. |
| `MainPage.CodeReview.cs` | Code Review Agent intake: pre-fills a review prompt in Coding Agent (from main). |
| `MainPage.ProjectHealth.cs` | Offline project health scan / architecture map dialog (`/health`, `/map`, More menu; from main). |
| `MainPage.CodingAgent.Proposals.cs` | Review dialogs for model-proposed file changes and commands. |
| `MainPage.Terminal.cs` | Project terminal dialog. |
| `MainPage.ImageStudio.cs` | Image Studio (local ComfyUI). |
| `MainPage.PluginsAndSkills.cs` | Plugins and Skills dialogs. |
| `MainPage.Companion.cs` | Desktop pet companion. |
| `MainPage.GitHub.cs` | GitHub workspace dialog: client ID, sign-in, repository list, clone into a project. |
| `MainPage.GitHub.SignIn.cs` | Device-flow polling and loading/refreshing/saving the GitHub credential. |
| `MainPage.GitHub.Changes.cs` | Commit/push review dialog for the active project. |

Other files in the UI project:

| File | What it does |
| --- | --- |
| `App.xaml` / `App.xaml.cs` | Application resources (colors, brushes, styles) and startup. Resources/design tokens are owned by the UI/design work. |
| `MarkdownUiRenderer.cs` | Turns model replies (Markdown, via Markdig) into native selectable XAML; HTML disabled, links checked before opening. |
| `CompanionStateStore.cs` | Saves the desktop pet companion's choice, position, and custom sprites to `companion.json`. Lives in the UI project because nothing in Core needs it. |
| `UiTheme.cs` | Design-token helpers for C#-built UI (colors, brushes, accent, toggle states, dialog sizing). Owned by the UI/design work; see `DESIGN.md`. |
| `GlobalUsings.cs` | Project-wide `using`s. |
| `Strings/en/Resources.resw`, `Assets/` | Strings and icons/splash. Pet sprites are linked in from the repo-level `assets/pets/`. |

### How the UI calls Core

There is no dependency-injection container or view-model layer yet. The
composition root is now centralized in `MainPageServices`, which creates the
same Core services and preserves the existing environment-variable profile
paths. `MainPage` receives that typed bundle once during construction and still
coordinates the current UI flows directly:

```text
MainPage (fields)                         ServerRock.Core
  MainPageServices     ─────────────────▶ typed service composition root
       │
       └── _provider    ─────────────────▶ OpenAiCompatibleProvider   (chat streaming, /models)
  _modelLibrary        ─────────────────▶ ModelLibraryService         (local runtimes, Hugging Face, ModelScope, Ollama pulls)
  _webSearchProvider   ─────────────────▶ WebSearchProvider           (DuckDuckGo HTML)
  _comfyImageProvider  ─────────────────▶ ComfyImageProvider          (local ComfyUI)
  _settingsStore       ─────────────────▶ JsonSettingsStore           (settings.json)
  _conversationStore   ─────────────────▶ ConversationStore           (conversations.json)
  _projectStore        ─────────────────▶ ProjectStore                (projects.json)
  _workspaceEditor     ─────────────────▶ WorkspaceFileEditor         (apply/undo reviewed file edits)
  _workspaceUndoStore  ─────────────────▶ WorkspaceUndoStore          (workspace-undo.json)
  _commandRunner       ─────────────────▶ WorkspaceCommandRunner      (approved one-off commands)
  _github*             ─────────────────▶ GitHubDeviceFlow / GitHubApiClient / GitHubCredentialStore / GitHubWorkspaceService
  (static helpers)     ─────────────────▶ ConversationArchive, ChatAttachmentPolicy, PdfAttachmentTextExtractor,
                                          WorkspaceFileContext, WorkspaceTextDiff, WorkspaceCommandProposalParser,
                                          ConversationTurnEditor, PluginCatalogService, SkillCatalogService
```

The typical pattern for a UI action:

1. A XAML event handler (`async void XxxClick`) calls
   `RunUiActionAsync(XxxAsync)`, which catches exceptions and shows them in the
   status banner (`RuntimeStatus`).
2. The `XxxAsync` method builds any dialog in code, then calls a Core service.
3. Results update page state (`_messages`, `_sessions`, `_projects`,
   `_settings`, …) and the page re-renders the affected list
   (`RenderRecent()`, `RenderProjects()`, `RebuildConversationView()`, …).
4. Settings changes are saved immediately with `SaveSettings()`.

Core methods take `CancellationToken`s; the page keeps one
`_requestCancellation` for the active chat request so the Stop button can
cancel it.

## ServerRock.Core (no UI)

Namespaces: `ServerRock.Core.Models`, `ServerRock.Core.Services`,
`ServerRock.Core.Infrastructure`, one folder each (`Models/`, `Services/`,
`Infrastructure/`).

### `Models/` – plain data

| File | What it is |
| --- | --- |
| `AppSettings.cs` | Every non-secret preference (endpoint, model, provider, pinned models, per-model speed samples, theme accent, ComfyUI workflow, GitHub App client ID, chat folders, …). Also `LocalModelPerformance.Key/AddSample` (speed averaging). |
| `ChatSession.cs` | One saved conversation: messages, project snapshot, mode, folder, archive state, branches/forks, import origin. |
| `ChatMessage.cs` | One message (role, text, attachments, web sources). `ForModel()` strips UI-only data before sending. |
| `ChatAttachment.cs` | A bounded text file, PDF text, or image attached to a message. |
| `ChatSource.cs` | One web search result shown under an answer. |
| `LocalProject.cs` | A named project: instructions + explicitly selected folder paths. |
| `WorkspaceModes.cs` | The two saved workspace IDs: `Chat` = "ChatBot", `CodingAgent` = "Coding Agent". Never change the values. |

### `Services/` – talk to the outside world

| File | What it does |
| --- | --- |
| `Services/OpenAiCompatibleProvider.cs` | Streams `/chat/completions` (SSE) and lists `/models` for any OpenAI-compatible server (Ollama, LM Studio, OpenRouter, …). Handles image content formats. |
| `Services/ModelLibraryService.cs` | Finds installed models across known local runtimes, searches Hugging Face and ModelScope, downloads/imports GGUF files into Ollama/LM Studio, pulls/deletes Ollama models. |
| `Services/CommandPaletteCatalog.cs` | UI-independent command-palette metadata and deterministic keyword search; native execution callbacks remain in the shell. |
| `Services/StartupRecoveryService.cs` | Bounded local queue/history/project/undo loading and interrupted-operation reconciliation for native startup; never starts external work. |
| `Services/WebSearchProvider.cs` | Opt-in DuckDuckGo HTML search; returns up to a handful of title/URL/snippet results. |
| `Services/ComfyImageProvider.cs` | Queues a user-supplied ComfyUI workflow on a **loopback-only** endpoint and polls for images. |
| `Services/GitHubDeviceFlow.cs` | GitHub App OAuth device flow (begin, poll with back-off, refresh). Also `GitHubHttpClientFactory` (no redirects, no cookies). |
| `Services/GitHubApiClient.cs` | Signed-in account, repository list, repository lookup; parses `owner/name` or GitHub URLs safely. |
| `Services/GitHubWorkspaceService.cs` | Local `git` clone/status/diff/commit/push for GitHub repos without putting tokens on the command line; blocks committing secret-looking files. |
| `Services/PluginCatalogService.cs` | Static catalog of supported plugins/integrations (search + categories). |
| `Services/LocalProjectHealthAnalyzer.cs` | Offline, bounded project health scan: manifests, entry points, risk markers, compact Markdown report. No model or network use. |
| `Services/SlashCommandCatalog.cs` | The `/` command list and the suggestion filter for the composer popup. |
| `Services/SkillCatalogService.cs` | Static catalog of built-in read-only "skills" (e.g. reverse-engineer) and their permissions. |

### `Infrastructure/` – local storage, files, processes, safety limits

| File | What it does |
| --- | --- |
| `JsonSettingsStore.cs` | Loads/saves `settings.json`. |
| `ConversationStore.cs` | Loads/saves `conversations.json`; refuses to overwrite a corrupt file. |
| `ProjectStore.cs` | Loads/saves `projects.json`. |
| `WorkspaceUndoStore.cs` | Last 20 approved file edits (`workspace-undo.json`) and undo with hash checks. |
| `GitHubCredentialStore.cs` | GitHub token storage: Windows DPAPI file, or Linux Secret Service via `secret-tool`. |
| `AppLogService.cs` | Rolling diagnostic log with secret/path redaction and zip export. |
| `AtomicFileWriter.cs` | Write-to-temp-then-rename used by the stores. |
| `BoundedFileReader.cs`, `BoundedHttpContent.cs` | Read files/HTTP bodies with hard byte limits before parsing. |
| `ConversationArchive.cs` | Import/export chat archives (native format and the old Electron export), with validation and duplicate/ID-collision planning. |
| `ConversationMarkdownExporter.cs` | Validated, human-readable Markdown transcript export with conversation metadata, messages, source links, and attachment names. |
| `ModelInstructions.cs` | Fixed system-message text: the Chat and Coding Agent prompts and the "untrusted data" warnings. |
| `LocalEndpoint.cs` | Whether an endpoint is on this device (loopback/localhost only). |
| `ProviderErrorText.cs` | Plain-language messages for model/image request failures. |
| `WorkspacePath.cs` | OS-correct path comparison for project folders. |
| `ExternalLinkPolicy.cs` | Which links in model replies may open (http/https only) and how their real host is shown. |
| `ConversationTurnEditor.cs` | "Edit an earlier message": replaces that turn and drops what followed. |
| `ChatAttachmentPrompt.cs` | Appends text/PDF attachments to the user's message as fenced "untrusted" blocks for the model. |
| `ChatAttachmentPolicy.cs` | Which files may be attached, size limits, image signature checks. |
| `PdfAttachmentTextExtractor.cs` | On-device PDF text extraction (PdfPig) with page/character bounds. |
| `WorkspaceFileContext.cs` | Reads a bounded, text-only sample of a project's selected folders for the model (skips secrets, VCS, deps, symlinks). |
| `WorkspaceFileEditor.cs` | Applies one reviewed text-file change inside a selected folder, with path containment and concurrency hash. |
| `WorkspaceTextDiff.cs` | Bounded line diff for the change-review preview. |
| `WorkspaceCommandProposalParser.cs` | Finds `<workspace-command>` proposals in a model reply. |
| `WorkspaceCommandRunner.cs` | Runs one approved command in a project folder with time/output limits. |
| `WorkspaceTerminalSession.cs` | Real pseudo-terminal (Porta.Pty + XTerm.NET screen buffer) scoped to a project folder. |

## Where data is stored

All app data goes under the user's platform app-data folders. On Linux,
`ApplicationData` is usually `~/.config` and `LocalApplicationData` is
`~/.local/share`; on Windows they are `%APPDATA%` and `%LOCALAPPDATA%`.

| Data | Location | Written by | Override (env var) |
| --- | --- | --- | --- |
| Settings (non-secret) | `ApplicationData/ServerRock/AI/settings.json` | `JsonSettingsStore` | `SRAI_SETTINGS_PATH` (explicit disposable/test path) |
| Conversations | `ApplicationData/ServerRock/AI/conversations.json` | `ConversationStore` | `SRAI_CONVERSATIONS_PATH` |
| Approved memory | `ApplicationData/ServerRock/AI/memory.json` | `MemoryStore` | `SRAI_MEMORY_PATH` (explicit disposable/test path) |
| Queued messages | `ApplicationData/ServerRock/AI/queued-messages.json` | `ChatMessageQueueStore` | `SRAI_QUEUE_PATH` (explicit disposable/test path) |
| Projects | `ApplicationData/ServerRock/AI/projects.json` | `ProjectStore` | – |
| Workspace undo history | `ApplicationData/ServerRock/AI/workspace-undo.json` | `WorkspaceUndoStore` | – |
| Companion (pet) prefs | `ApplicationData/ServerRock/AI/companion.json` | `CompanionStateStore` (UI project) | `SRAI_COMPANION_STATE_PATH` |
| Logs | `LocalApplicationData/ServerRock/AI/logs/` | `AppLogService` | `SRAI_LOG_DIRECTORY` |
| GitHub token (Windows) | `LocalApplicationData/ServerRock/AI/github-user-token.bin`, DPAPI-encrypted for the current user | `GitHubCredentialStore` | – |
| GitHub token (Linux) | Desktop keyring (Secret Service) via `secret-tool`; needs a D-Bus session | `GitHubCredentialStore` | – |
| OpenRouter API key | Memory only (`MainPage._openRouterApiKey`); never written to disk | – | – |
| Temporary chats | Memory only; never written to `conversations.json` | – | – |

Stores read with a size limit, write atomically, and leave a corrupt file
untouched (reporting an error) instead of replacing it.

## Tests

`ServerRock.Core.Tests` is a plain console app. `Program.cs` is only the runner:
it calls each test area in order and the run stops at the first failure
(`Check(condition, description)` throws).

| File | Covers |
| --- | --- |
| `TestSupport.cs` | `Check`, `RunGit`, `DeleteDirectoryTree`, `ReadElectronFixtureAsync`, and the fakes `StubHandler` (HTTP) and `NonSeekableReadStream`. |
| `DiagnosticsTests.cs` | Log redaction and export. |
| `CatalogTests.cs` | Plugin, skill, and slash-command catalogs. |
| `ProjectHealthTests.cs` | Offline project health scan bounds and findings. |
| `ModelLibraryTests.cs` | Local runtime discovery, Hugging Face / ModelScope, GGUF import, Ollama pull/delete. |
| `ChatProviderTests.cs` | OpenAI-compatible discovery, streaming, request shape, images, limits. |
| `ImageGenerationTests.cs` | ComfyUI generation and cancellation. |
| `AttachmentTests.cs` | Attachment rules and PDF text extraction. |
| `BoundedReadTests.cs` | Size-limited HTTP and file reads. |
| `ConversationArchiveTests.cs` | Archive import/export, legacy Electron format. |
| `WebSearchTests.cs` | DuckDuckGo result parsing. |
| `GitHubTests.cs` | Device flow, credentials, repository parsing, API limits. |
| `ChatMessageTests.cs` | Editing an earlier turn; model-facing message shape. |
| `LocalStorageAndWorkspaceTests.cs` | Stores, workspace context, git review, commands, terminal, editor, diff, undo (one shared temp folder). |
| `ExternalLinkTests.cs` | Which reply links may open and how they are labeled. |

To add a check, put it in the matching `*Tests.cs` file. A new area gets a new
`internal static class XxxTests` with `Run()`/`RunAsync()` and one line in
`Program.cs`. HTTP is faked with `StubHandler`; storage tests use temp folders.
Fixtures for the legacy Electron archive format are in `Fixtures/`.

## Where do I find…?

| I'm looking for… | File |
| --- | --- |
| App startup, single-instance check | `ServerRock.Native/Platforms/Desktop/Program.cs`, `App.xaml.cs` |
| The page layout | `ServerRock.Native/MainPage.xaml` |
| The sidebar | `ServerRock.Native/SidebarView.xaml` (shell layout), `ProjectNavigationView.xaml` (project context/files), `ConversationNavigationView.xaml` (folders/import/export/recents), `MainPage.Sidebar.cs` (wiring, More menu) |
| Health scan / architecture map | `ServerRock.Native/MainPage.ProjectHealth.cs`, `ServerRock.Core/Services/LocalProjectHealthAnalyzer.cs` |
| Code Review Agent | `ServerRock.Native/MainPage.CodeReview.cs` |
| Colors, brushes, styles, design tokens | `ServerRock.Native/App.xaml`, `UiTheme.cs`, `DESIGN.md` |
| Page fields and shared visual-control accessors | `ServerRock.Native/MainPage.xaml.cs` |
| Header visual tree and header events | `ServerRock.Native/MainPageHeader.xaml` / `.xaml.cs` |
| Constructor, lifecycle recovery, and event wiring | `ServerRock.Native/MainPage.Lifecycle.cs` |
| What happens when I press Send | `ServerRock.Native/MainPage.Chat.cs` (`SendClick`) |
| The system prompt / what's sent to the model | `ServerRock.Core/Infrastructure/ModelInstructions.cs` (text), `ServerRock.Native/MainPage.Chat.cs` (`BuildProviderMessages`), `ServerRock.Core/Infrastructure/ChatAttachmentPrompt.cs` |
| Chat streaming HTTP | `ServerRock.Core/Services/OpenAiCompatibleProvider.cs` |
| How messages are drawn, copy/edit/retry | `ServerRock.Native/MainPage.Messages.cs` |
| Markdown rendering of replies, link safety | `ServerRock.Native/MarkdownUiRenderer.cs`, `ServerRock.Core/Infrastructure/ExternalLinkPolicy.cs` |
| Branches ‹ › and forks | `ServerRock.Native/MainPage.Branches.cs` |
| The model picker / hotswap menu | `ServerRock.Native/MainPage.ModelLibrary.cs` (`ModelClick`, `ModelHotswapClick`) |
| The Model Library dialog | `ServerRock.Native/MainPage.ModelLibrary.cs` (`ModelLibraryClickAsync`), `ServerRock.Core/Services/ModelLibraryService.cs` |
| "Local ready / Not connected" status | `ServerRock.Native/MainPage.Connection.cs` |
| Settings dialog | `ServerRock.Native/MainPage.Settings.cs` (`SettingsDialogAsync`) |
| Temperature (Advanced) | `ServerRock.Native/MainPage.Settings.cs` (`AdvancedClickAsync`) |
| Tools row, web search / temporary chat / smart rewrite toggles | `ServerRock.Native/MainPage.Composer.cs` |
| "Is this endpoint local?" | `ServerRock.Core/Infrastructure/LocalEndpoint.cs` |
| Error text shown when a model request fails | `ServerRock.Core/Infrastructure/ProviderErrorText.cs` |
| The mode IDs ("ChatBot" / "Coding Agent") | `ServerRock.Core/Models/WorkspaceModes.cs` |
| Attachments (+ button, chips, limits) | `ServerRock.Native/MainPage.Attachments.cs`, `ServerRock.Core/Infrastructure/ChatAttachmentPolicy.cs` |
| Slash commands (`/help`, `/mode`, …) | `ServerRock.Core/Services/SlashCommandCatalog.cs` (list), `ServerRock.Native/MainPage.SlashCommands.cs` (what each does) |
| Voice input and spoken replies | `ServerRock.Native/MainPage.Voice.cs` |
| Chat / Coding Agent switch, keyboard shortcuts | `ServerRock.Native/MainPage.Navigation.cs` |
| Narrow-window drawer, toolbar wrapping | `ServerRock.Native/MainPage.Layout.cs` |
| Recents list, rename/archive/delete a chat | `ServerRock.Native/MainPage.Recents.cs` |
| Chat folders | `ServerRock.Native/MainPage.ChatFolders.cs` |
| Import / export chats | `ServerRock.Native/MainPage.ImportExport.cs`, `ServerRock.Core/Infrastructure/ConversationArchive.cs` |
| Projects and their folders | `ServerRock.Native/MainPage.Projects.cs` |
| Coding Agent file list, file editor, undo history | `ServerRock.Native/MainPage.CodingAgent.cs` |
| Reviewing a proposed file change or command | `ServerRock.Native/MainPage.CodingAgent.Proposals.cs` |
| Project terminal | `ServerRock.Native/MainPage.Terminal.cs`, `ServerRock.Core/Infrastructure/WorkspaceTerminalSession.cs` |
| Image Studio | `ServerRock.Native/MainPage.ImageStudio.cs`, `ServerRock.Core/Services/ComfyImageProvider.cs` |
| Plugins and Skills dialogs | `ServerRock.Native/MainPage.PluginsAndSkills.cs` |
| The pet companion | `ServerRock.Native/MainPage.Companion.cs`, `CompanionStateStore.cs` |
| GitHub dialog, clone | `ServerRock.Native/MainPage.GitHub.cs`, `ServerRock.Core/Services/GitHub*.cs` |
| GitHub sign-in polling, saved token | `ServerRock.Native/MainPage.GitHub.SignIn.cs`, `ServerRock.Core/Infrastructure/GitHubCredentialStore.cs` |
| GitHub commit/push review | `ServerRock.Native/MainPage.GitHub.Changes.cs`, `ServerRock.Core/Services/GitHubWorkspaceService.cs` |
| Saving settings / chats / projects | `ServerRock.Core/Infrastructure/*Store.cs` |
| Core tests | `ServerRock.Core.Tests/*Tests.cs` (see Tests above) |

## Visual decomposition notes

The native page is intentionally split into focused partial classes and UserControls.
`MainPage.xaml` hosts the shell, while `SidebarView`, `MainPageHeader`,
`CodingWorkspaceBar`, `ConversationSurface`, `ComposerInputView`, and
`ConversationNavigationView` own independent visual regions. The controls forward
events and expose compatibility accessors so persistence, provider, and pet behavior
remain owned by their existing feature code. This keeps the decomposition reversible
and avoids mixing UI extraction with data or runtime changes.
