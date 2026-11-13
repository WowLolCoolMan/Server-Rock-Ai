# Skai Desktop Agent and Architecture Audit

Date: 2026-11-13  
Scope: `codex/skai-current-worktree`, working tree, Electron replacement path,
.NET agent host, Core provider layer, AirLLM integration, and the two active
implementation chats.

## Owner conclusion

The product direction is good, but the current implementation is not ready to
merge or call complete. The safe target is:

```text
Electron + React renderer
        -> preload/main-process bridge
        -> one persistent C#/.NET agent host
        -> SkaiDesktop.Core
        -> local providers, AirLLM, storage, and guarded tools
```

Uno remains a rollback and parity surface until the Electron gates pass. Rust is
not part of the approved runtime. AirLLM is a local execution capability inside
the same host contract, not a second user-visible app and not a silent cloud
fallback.

The agents are therefore constrained to finish their current bounded slices,
then pause. New work is admitted only after the host boundary, AirLLM reachability,
security, cancellation, and provider identity are coherent.

## Review basis and status vocabulary

This audit separates **user decision**, **repository observation**, **proposal**,
and **evidence gap**. A passing typecheck or build is not treated as proof of a
working user journey.

Reviewed:

- `README.md`, `docs/ARCHITECTURE.md`, `docs/ELECTRON_MIGRATION_CUTOVER.md`,
  `docs/CAPABILITY_MATRIX.md`, and `docs/FUTURE_FEATURES.md`.
- `services/agent-host/Protocol/AgentHostLoop.cs` and the Electron bridge.
- `native/SkaiDesktop.Core/Services/AirLlmProvider.cs`,
  `services/airllm/skai_airllm.py`, and AirLLM setup documentation.
- Current uncommitted Electron Chat retry and C# voice changes.
- Active chats `Add future features list` and `Find a lightweight TTS model`.
- Checks run during this audit: desktop typecheck, desktop build, syntax audit,
  Electron boundary audit, and Core tests passed; agent-host build failed because
  a live `ServerRock.AgentHost` process held its output DLL.

## Decision register

| ID | Decision | Source/status | Audit judgment | Required closure |
| --- | --- | --- | --- | --- |
| D-01 | Local-first desktop app with explicit hosted-provider use | User requirement; accepted | Sound product boundary | Every hosted route needs visible identity, consent, and no silent remote fallback |
| D-02 | Use AirLLM whenever a local model exceeds the computer's practical limits | User requirement; needs refinement | Good outcome, unsafe as currently implemented | Route through the Electron/.NET host, gate by model/runtime capability, show the fallback, and keep it local-only |
| D-03 | Provider choice should be understandable to non-experts | User requirement; partially implemented | Correct UX goal | Store provider identity separately from endpoint/model and expose an intent-based setup flow |
| D-04 | Use the existing visual assets with restrained decoration | User requirement; implemented in the UI slice | Sound; the wolf mark is appropriate as a compact brand anchor | Keep dense work surfaces content-first and continue visual acceptance checks |
| D-05 | Parent agent owns future-feature implementation; UI chats receive completion handoffs | User requirement; accepted | Correct coordination boundary | One vertical slice, one owner, one evidence bundle; no parallel edits to the same seam |
| D-06 | Electron + React with one persistent C#/.NET host | User decision and repository canon; accepted | Correct near-term architecture for this codebase | Remove Rust voice runtime references and keep all native capabilities behind the host |
| D-07 | Push a non-main branch and use a draft PR | User requirement; completed | Correct release safety, but the snapshot was too broad | Curate follow-up commits; do not merge the 522-file mixed worktree snapshot without review |

## Actual architecture observed

The approved replacement path is present and mostly coherent for ordinary
OpenAI-compatible chat:

```text
Electron renderer -> preload -> Electron main -> JSON-lines C# host
                                                   -> SkaiDesktop.Core
                                                   -> OpenAI-compatible endpoint
                                                   -> local stores and guarded tools
```

The AirLLM path is not on that route. It currently exists here:

```text
Legacy Uno MainPage.Chat
    -> SkaiDesktop.Core AirLlmProvider
    -> Python JSON-lines worker
    -> AirLLM AutoModel
```

The Electron host's `chat.complete` handler currently constructs only an
`OpenAiCompatibleProvider`. Therefore the approved desktop shell cannot use the
AirLLM implementation that the earlier work reports as integrated.

The voice work is converging on the correct C# host boundary, but the checkout
still contains Rust-era generated `native/voice-host/target` output and UI copy
that says future model packs depend on a Rust runtime. That is architecture drift,
not harmless polish.

## Findings

### Critical: AirLLM is unreachable from the approved desktop path

**Observation.** `AirLlmProvider` is called by `native/ServerRock.Native/MainPage.Chat.cs`.
`services/agent-host/Protocol/AgentHostLoop.cs` handles Electron `chat.complete`
with `OpenAiCompatibleProvider` and has no AirLLM route.

**Condition.** A user runs the approved Electron app, selects or needs a
Hugging Face model that does not fit the normal runtime, and sends a message.

**Consequence.** The feature promised to the user is absent from the app that
the repository calls the replacement target. The user either gets a normal
provider failure or must use the legacy Uno surface.

**Minimal correction.** Put the provider selection and AirLLM adapter behind the
C# host/Core contract. `chat.complete`, `models.list`, runtime health, operation
provenance, and cancellation must all use the same provider strategy. Keep the
legacy Uno adapter only as a compatibility caller until cutover.

**Closure check.** A disposable Electron run sends a compatible Hugging Face
model request through the host, records `normal -> AirLLM` locally, returns a
response, and never uses a hosted endpoint. A failure must identify whether the
runtime is unavailable, the model is unsupported, storage is insufficient, or
the model itself failed.

### Critical: arbitrary model code is currently trusted inside the AirLLM worker

**Observation.** `services/airllm/skai_airllm.py` sets `trust_remote_code=True`
and accepts a user-supplied Hugging Face model ID. AirLLM's upstream quickstart
also loads models by repository ID and warns that the first run decomposes and
stores model layers locally. Upstream issue #293 documents the remote-code risk.

**Condition.** A user selects a repository whose configuration/tokenizer/model
code is not trusted, and Skai starts the Python worker.

**Consequence.** Model-repository Python can execute with the worker's local
process permissions. This conflicts with Skai's local safety and provenance
claims and is more serious than an ordinary provider compatibility error.

**Minimal correction.** Default to native Transformers implementations with
remote code disabled. For models that genuinely require custom code, require an
explicit first-use consent showing the repository, revision, publisher/source,
and permissions; run the worker in a reduced-permission isolated process with
no inherited secrets; persist the decision and revision; and fail closed when
the model changes. The UI must never present an arbitrary repository as an
ordinary installed model without this state.

**Closure check.** A malicious or custom-code fixture is refused by default;
an explicitly approved pinned fixture runs in the isolated worker; environment
secrets are absent; revoking approval blocks the next request; and provenance is
visible in the model/runtime status.

### Important: automatic fallback is underspecified and can mislead users

**Observation.** The current native fallback uses exception-text matching such
as `memory`, `does not fit`, or `model compatibility`, and only falls back when a
model string looks like a Hugging Face ID. There is no corresponding Electron
host route, preflight resource check, or durable provider identity in the new
shell.

**Condition.** A normal provider reports an unrelated error containing a memory
word, or a selected model is not a Hugging Face/AirLLM-compatible model.

**Consequence.** Skai can retry through a provider that cannot load the model,
hide the original cause, or leave the user believing that any local provider can
be converted into AirLLM. A silent model/runtime switch also makes troubleshooting
and quality comparison harder.

**Minimal correction.** Define an explicit local fallback policy: preflight the
selected model/provider, check AirLLM installation and disk budget, classify only
known capacity errors, reserve a bounded retry, preserve the original error, and
show the chosen local runtime and reason. Never fall back from a hosted endpoint
to another route without explicit consent.

**Closure check.** Tests cover normal success, known capacity failure, unrelated
failure, unsupported model ID, missing runtime, cancellation, and fallback
failure. Each outcome records provider, model, reason, and whether a second
attempt was made.

### Important: the host lifecycle and long-inference control are not release-safe

**Observation.** Electron launches `dotnet run` and calls `host.kill()` during
window shutdown. The current `chat.complete` path uses `CancellationToken.None`.
During this audit, a live `ServerRock.AgentHost` process held
`services/agent-host/bin/Debug/net10.0/SkaiDesktop.Core.dll`, causing a build to
fail after ten copy retries.

**Condition.** A long local/AirLLM request is active, the window closes, or a
smoke test ends while the `dotnet run` child and actual host process have
different lifetimes.

**Consequence.** The user cannot reliably stop frontier inference; requests can
outlive the UI; builds and tests become nondeterministic; and the app can report
that work stopped while the native process remains alive.

**Minimal correction.** Use a supervised host lifecycle with a direct executable
or process-tree shutdown, request timeouts, cancellation propagation, bounded
pending requests, and explicit `started/active/cancelled/failed/completed` state.
The smoke test must terminate the actual host and assert no child remains.

**Closure check.** Start, cancel, timeout, crash, and close-window tests leave no
agent-host process, resolve pending calls, and permit an immediate clean rebuild.
AirLLM receives the same cancellation and deadline contract.

### Important: the current branch snapshot is not a healthy merge unit

**Observation.** The pushed commit contains 522 files and mixes the native-core
rename, UI migrations, handoff archives, AirLLM, presets, and active agent work.
The current working tree is again dirty with voice/UI changes and generated
`artifacts/`/Rust target output.

**Consequence.** Reviewers cannot distinguish architectural changes from
generated material or unrelated slices. A green check on one surface can hide a
broken boundary on another.

**Minimal correction.** Keep the branch non-main, but create reviewable follow-up
commits by boundary: host lifecycle/provider contract, AirLLM runtime/security,
C# voice cleanup, and UI slices. Do not stage generated artifacts. Each commit
gets its own focused checks and evidence.

**Closure check.** `git status` contains only intentional source/docs changes;
the PR summary maps each commit to an acceptance check; no generated output,
Rust target, `dist`, or process-lock residue is present.

### Improvement: provider UX is not yet an intent-based provider system

**Observation.** The Electron model surface derives provider labels from model
names and sends an endpoint/model pair to the host. The legacy native settings
have friendlier copy, but the replacement shell does not yet persist a provider
identity or explain local/large-model/hosted choices as user goals.

**Correction.** Add a small provider setup flow after the host contract is stable:
“Use models already on this computer,” “Use a very large model locally,” and
“Connect a hosted service.” Each choice maps to a typed provider profile with
health, capabilities, privacy, install state, and a clear next action.

## Agent decision audit

### `Add future features list`

The current decision to finish Chat retry as one isolated slice is healthy. The
agent preserves parallel TTS edits, uses the existing `chat.complete` contract,
and reports focused tests. It must stop after that checkpoint. It must not add
new routes, provider logic, AirLLM behavior, or broad UI polish until the host
contract is repaired.

### `Find a lightweight TTS model`

The decision to consolidate voice into the existing C# host is healthy and
matches the owner decision. The earlier Rust sidecar was a wrong turn for this
repository. The agent must finish the cleanup rather than leave a dual-runtime
story, and its smoke test must prove lifecycle cleanup as well as input
validation. Kokoro/Pocket TTS should be future C# adapters, not promises that
depend on a removed Rust runtime.

### Parent/orchestrator decisions

The original push preserved user work and protected `main`, which was correct.
The mistake was treating the whole active worktree as one releasable commit.
The current correction is to keep the branch, avoid destructive rollback, and
use narrow follow-up commits plus architecture gates before any merge.

## Priority correction sequence

1. Finish and checkpoint the two current bounded slices; no new scope.
2. Remove generated artifacts and all Rust voice residue; make host lifecycle
   shutdown deterministic.
3. Implement the provider strategy in the C# host so Electron `chat.complete`
   can reach AirLLM under the same contract as normal local providers.
4. Add AirLLM provenance, model compatibility, trust/consent, disk/runtime
   health, cancellation, and bounded fallback tests.
5. Add the intent-based provider setup flow in Electron and durable provider
   identity.
6. Re-run packaged Electron acceptance, Core tests, host smoke, and a real
   disposable AirLLM model test before discussing merge.

## Open evidence gaps

- No Electron end-to-end AirLLM request has been demonstrated.
- No packaged installer contains or provisions the AirLLM runtime.
- No resource/disk estimate or cleanup policy has been verified for layer shards.
- No safe policy has been demonstrated for AirLLM remote/custom model code.
- No clean host cancellation/timeout/close-window test has passed.
- No release review has separated the large mixed branch snapshot into focused
  acceptance units.

## Acceptance bar before merge

The feature is not “done” when a Python worker starts. It is ready for review
only when the approved Electron path can select and explain a local AirLLM
attempt, preserve local-only privacy, cancel it, recover from failure, and leave
no orphan process or ambiguous provider state. The user must be able to tell
what Skai is doing, why it chose that runtime, what it downloaded, and how to
stop or undo the choice.
