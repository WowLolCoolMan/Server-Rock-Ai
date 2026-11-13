# Server Rock native integration — exact file map

This is a file-targeted development blueprint, not a compiled change set.

## Inspected existing code

- `native/SkaiDesktop.Core/Services/SkillCatalogService.cs` — `SkillDefinition` record, three built-ins: `reverse-engineer`, `project-audit`, `release-readiness`. Static `Find()`.
- `native/ServerRock.Native/MainPage.PluginsAndSkills.cs` — Plugin dialog + Skills dialog. Skills selection loads prompt text into composer; no installer/update UI. Skills dialog has Done and Close.
- `native/SkaiDesktop.Core/Services/PluginCatalogService.cs` — “Skills Pack” listed, not marked built-in; plugin entries use `IsBuiltIn`/availability metadata.
- `native/SkaiDesktop.Core/Services/GitHubApiClient.cs` — existing credentialed GitHub account and repo APIs, GitHub URL parsing. Reuse its transport/security guidance; public skill fetch should not require signed-in GitHub by default.
- `native/ServerRock.Native/MainPage.Chat.cs` — `BuildProviderMessages` builds system context; integrate skill-selection metadata here, but keep retrieval and policy checks in Core.
- `native/SkaiDesktop.Core/Services/OpenAiCompatibleProvider.cs` — streamed chat transport; do not confuse with full tool-call runtime.
- `native/ServerRock.Native/MainPage.ModelLibrary.cs` and `native/SkaiDesktop.Core/Services/ModelLibraryService.cs` — installed models and catalog, provider/endpoint identity. Integrate capability checks without collapsing same-name models across hosts.
- `native/SkaiDesktop.Core/Services/ComfyImageProvider.cs` — queue and poll user-supplied API-format ComfyUI JSON against loopback.
- `native/ServerRock.Native/MainPage.ImageStudio.cs` — separate ComfyUI dialog; reuse for workflow skills.
- `native/ServerRock.Native/MainPage.CodingAgent.Proposals.cs` — review-before-execute command/file proposals; retain or extend this approval contract.
- `native/SkaiDesktop.Core.Tests/` — console test harness; add marketplace/skill routing regression fixtures and decide whether to adopt standard xUnit without breaking current CI.
- `docs/ARCHITECTURE.md`, `docs/PRODUCTION_READINESS_GAPS.md`, `docs/NATIVE_RELEASE_ACCEPTANCE.md` — update canonical records when code lands.

## New Core services to create

Suggested filenames (design, not existing):

```
native/SkaiDesktop.Core/Skills/
  SkillManifest.cs                 # metadata + permissions + compatibility
  SkillRegistry.cs                 # discoverable + installed entries
  SkillStorage.cs                  # profile-owned store and version lockfile
  SkillSourceGitHub.cs             # public GitHub manifests/tree metadata
  SkillInstaller.cs                # staging, validation, atomic update, rollback
  SkillUpdateChecker.cs            # per-skill subtree SHA and conflict detection
  SkillTrustPolicy.cs              # permissions and provenance
  SkillCompatibilityResolver.cs    # runtime tools/OS/model feature checks
  SkillRouter.cs                   # candidate search and ranked selection
  SkillContextComposer.cs          # token-budgeted instruction injection
  SkillExecutionEngine.cs          # permission-bound tools, status, cancellation
  SkillScouter.cs                  # opt-in GitHub discovery suggestions
  SkillCreator.cs                  # draft->lint->test->publish local skill
  SkillActivityStore.cs            # redacted history and failure reports
```

Do not add Python as a mandatory runtime dependency to Server Rock. Port the `marketplace.py` reference logic to .NET 10 and use native `HttpClient` or an isolated `git` process adapter with the same trust checks. Keep UI-independent logic in `SkaiDesktop.Core`.

## Core interfaces

```csharp
public interface ISkillRegistry {
    Task<IReadOnlyList<SkillRecord>> SearchAsync(string query, CancellationToken ct);
    Task<SkillRecord?> GetInstalledAsync(string id, CancellationToken ct);
}
public interface ISkillMarketplace {
    Task<InstallPreview> PreviewInstallAsync(string id, CancellationToken ct);
    Task<InstallResult> InstallAsync(string id, UserApproval approval, CancellationToken ct);
    Task<IReadOnlyList<SkillUpdate>> CheckUpdatesAsync(CancellationToken ct);
    Task<UpdatePreview> PreviewUpdateAsync(string id, CancellationToken ct);
    Task<InstallResult> ApplyUpdateAsync(string id, UserApproval approval, CancellationToken ct);
}
public interface ISkillRouter {
    Task<IReadOnlyList<SkillSelection>> SelectAsync(TaskContext task, ModelCapabilities model, int tokenBudget, CancellationToken ct);
}
```

`UserApproval` must be validated by an application-level permission service, not an AI-generated boolean or text token. All long operations accept `CancellationToken`; post cancellation, clean staging but retain good previous install.

## UI navigation redesign

Move beyond current `ContentDialog` list. Suggest a full-page native `MarketplaceView` (Uno XAML + code-behind or MVVM) reachable from sidebar and `/skills`. Tabs: **Discover | Installed | Updates | My Skills | Activity**. Search and filter remain fixed in viewport; details side pane scrolls independently; no behind-page selections. Each card includes: name, origin, version SHA, license, platform, tool permissions, current/available update state, Verified/Unreviewed badge, install/update actions. Distinguish fetching vs installing vs enabling.

“Use skill” should enqueue a selected skill for the **current request**, rather than only pasting prompt text. Offer “Always ask”, “For this project”, and “Manual only” activation modes; never silently elevate permissions. Keep a manual choice for deterministic behavior.

## Skill routing and provider integration

1. Parse user task and selected project context locally.
2. Retrieve top 10 candidate metadata matches from registry; filter by OS, available tools, model capability, permission and requested files.
3. Rerank top 5 on short metadata, select 0–3. User can override.
4. Load only selected SKILL.md and necessary references; cap tokens. Do not load 250 skills into system prompt.
5. Resolve dependencies explicitly; ask before any install/download/script execution.
6. Send to chosen model. If tool-calling unsupported, run instruction-only workflow or ask user to choose a compatible model; never fake action success.
7. Log selection rationale and results locally, redacting private snippets and credentials.

## Release acceptance to add in native CI

- Core tests for parse, search, compatibility, path safety, arbitrary Git metadata, permission denial, canceled install, duplicate detection, SHA comparison, rollback, unreadable store recovery.
- Mocked GitHub REST and local fixture Git repositories; run without network in normal CI.
- Real GitHub install/update smoke only in opt-in integration job (rate limits and network failures distinguished).
- Windows/Linux UI automation for search, detail, consent, install, offline, available update, upgrade conflict, uninstall, keyboard focus.
- Genuine local and hosted model skill selection tests with the same user scenario, including tool-incapable model fallback.
- Retain existing release-gate checks; new marketplace acceptance is additive.
