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
- Current Electron Chat retry, C# voice, provider-contract, and AirLLM host changes.
- Active chats `Add future features list` and `Find a lightweight TTS model`.
- Checks run during this audit: desktop typecheck, desktop build, syntax audit,
  Electron boundary audit, and Core tests passed; the first agent-host build
  was blocked by a live `ServerRock.AgentHost` process, then passed after the
  host was shut down cleanly.

## Decision register

| ID | Decision | Source/status | Audit judgment | Required closure |
| --- | --- | --- | --- | --- |
| D-01 | Local-first desktop app with no cloud or hosted model routes | Latest user override; accepted | Sound privacy boundary; older hosted-provider copy is stale | Every model route must be loopback-only and fail closed for remote endpoints |
| D-02 | Use AirLLM whenever a local model exceeds the computer's practical limits | User requirement; needs refinement | Good outcome, unsafe as currently implemented | Route through the Electron/.NET host, gate by model/runtime capability, show the fallback, and keep it local-only |
| D-03 | Local provider choice should be understandable to non-experts | User requirement; partially implemented | Correct UX goal | Store local provider identity separately from endpoint/model and expose local intent-based setup flow |
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

The AirLLM adapter is now reachable from that route, but its release contract
is not closed. The intended path is:

```text
Electron chat.complete
    -> .NET Agent Host provider strategy
    -> SkaiDesktop.Core AirLlmProvider
    -> Python JSON-lines worker
    -> AirLLM AutoModel
```

The host now supports explicit AirLLM selection and a bounded local capacity
fallback, returns provider/fallback provenance, and is being restored to a
strict loopback-only endpoint boundary. Real Electron inference, runtime health,
cancellation, and trust/consent evidence are still missing.

The voice work is now on the correct C# host boundary, with the Rust voice
source, target output, and stale runtime wording removed. Host lifecycle and
future model-pack installation remain separate release gates.

## Findings

### Corrected in this branch: AirLLM is reachable from the approved desktop path

**Observation.** `services/agent-host/Protocol/AgentHostLoop.cs` now routes
explicit `AirLLM` requests through `AirLlmProvider` and retries only a local
Hugging Face-shaped model after a classified capacity failure. The Electron
request carries provider identity and the response reports the selected route.

**Condition.** A user runs the approved Electron app, selects or needs a
Hugging Face model that does not fit the normal runtime, and sends a message.

**Consequence.** The original reachability gap is closed, but the feature is
not yet release-ready: the runtime may be absent, a real model journey has not
been exercised, and long-running inference still has no request cancellation.

**Remaining correction.** Complete the shared provider strategy across
`chat.complete`, `models.list`, runtime health, operation provenance, and
cancellation. Keep the legacy Uno adapter only as a compatibility caller until
cutover.

**Closure check.** A disposable Electron run sends a compatible Hugging Face
model request through the host, records `normal -> AirLLM` locally, returns a
response, and never uses a hosted endpoint. A failure must identify whether the
runtime is unavailable, the model is unsupported, storage is insufficient, or
the model itself failed.

### Critical: arbitrary model code is currently trusted inside the AirLLM worker

**Observation.** `services/airllm/skai_airllm.py` now defaults
`trust_remote_code` to false, but accepts a user-supplied Hugging Face model ID
and does not yet pin provenance or isolate the worker. AirLLM's upstream
quickstart loads models by repository ID and warns that the first run decomposes
and stores model layers locally. Upstream issue #293 documents the remote-code
risk, so the flag alone is not sufficient evidence of safety.

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

### Important: automatic fallback is improved but not release-complete

**Observation.** The host now carries provider identity, checks that the route
is local before fallback, checks that the AirLLM Python imports are available,
and preserves the fallback failure context. Capacity classification is still
based on bounded exception text, and there is no disk/model preflight.

**Condition.** A normal provider reports an unrelated error containing a memory
word, or a selected model is not a Hugging Face/AirLLM-compatible model.

**Consequence.** Skai can still misclassify a provider error containing a memory
word, or start a large download without enough disk. A silent model/runtime
switch would also make troubleshooting and quality comparison harder.

**Minimal correction.** Define an explicit local fallback policy: preflight the
selected model/provider, check AirLLM installation and disk budget, classify only
known capacity errors, reserve a bounded retry, preserve the original error, and
show the chosen local runtime and reason. Remote endpoints must be rejected
before any request is sent.

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
The current working tree contains intentional voice/UI/host changes; generated
artifacts and Rust voice targets have been removed from the checkout.

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

**Observation.** The Electron shell now persists and sends local provider
identity, and the host returns it with settings and discovered models. The
replacement shell still does not explain “models on this computer” versus
“very large models locally through AirLLM” as user goals or provide the setup
flow.

**Correction.** Add a small local provider setup flow after the host contract is
stable: “Use models already on this computer” and “Use a very large model
locally.” Each choice maps to a typed provider profile with health, capabilities,
privacy, install state, and a clear next action. Do not expose a hosted option.

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
2. Keep generated artifacts and all Rust voice residue out; make host lifecycle
   shutdown deterministic.
3. Complete AirLLM provenance, model compatibility, trust/consent, disk/runtime
   health, cancellation, and bounded fallback tests.
4. Add the intent-based provider setup flow in Electron on top of the durable
   provider identity.
5. Re-run packaged Electron acceptance, Core tests, host smoke, and a real
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
