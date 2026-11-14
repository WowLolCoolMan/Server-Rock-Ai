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
  Electron boundary audit, local-only audit, legacy/native build, host smoke,
  and Core tests passed. A live agent-host process was found after UI testing
  and was terminated before handoff; no agent-host process remains.

## Decision register

| ID | Decision | Source/status | Audit judgment | Required closure |
| --- | --- | --- | --- | --- |
| D-01 | Local-first desktop app with first-class explicit cloud services | Latest user override; accepted | Sound if local/AirLLM and cloud provider adapters are separate and visible; arbitrary unvalidated endpoints remain unsafe | Add typed route policy, provider adapters, auth storage, explicit data-route labels, and fail-closed route selection |
| D-02 | Use AirLLM whenever a local model exceeds the computer's practical limits | User requirement; accepted with compatibility limit | Sound when the model is a valid Hugging Face/local-directory AirLLM reference; not possible to infer a mapping from arbitrary Ollama tags | Keep fallback local, visible, bounded, cancellable, and explicit about unsupported model identifiers |
| D-03 | Provider choice should be understandable to non-experts | User requirement; partially implemented | Correct UX goal; wording is now local-intent based but the complete Electron setup flow is still missing | Finish the intent-based setup flow for local, AirLLM, cloud model, cloud voice, and other supported services without exposing provider jargon |
| D-04 | Use the existing visual assets with restrained decoration | User requirement; implemented in the UI slice | Sound; the wolf mark is appropriate as a compact brand anchor | Keep dense work surfaces content-first and continue visual acceptance checks |
| D-05 | Parent agent owns future-feature implementation; UI chats receive completion handoffs | User requirement; accepted | Correct coordination boundary | One vertical slice, one owner, one evidence bundle; no parallel edits to the same seam |
| D-06 | Electron + React with one persistent C#/.NET host | User decision and repository canon; accepted | Correct near-term architecture for this codebase | Remove Rust voice runtime references and keep all native capabilities behind the host |
| D-07 | Support cloud providers and authentication | Latest user override; accepted with auth distinction | Correct product direction; API keys, OAuth/account sign-in, and service-specific credentials are different auth contracts | Implement provider adapters with OS-backed secrets, explicit route/data labels, PKCE/OAuth where supported, revocation, usage/error states, and no secret in settings/logs |
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
fallback, returns provider/fallback provenance, enforces loopback-only model and
image endpoints, and propagates request deadlines/cancellation into the local
provider path. The shared Core and legacy/native surface now use the same
strict HTTP(S) loopback definition, including rejection of URL credentials and
fragments. Real Electron AirLLM inference, runtime health UX, disk preflight,
and trust/consent evidence are still missing. The newly approved cloud provider
routes are not implemented yet; the current transport intentionally rejects
them until typed provider and credential boundaries are added.

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
been exercised, and the worker still lacks provenance/isolation closure.

**Remaining correction.** Add model/runtime health and disk preflight, a real
Electron AirLLM acceptance path, and explicit provenance/isolation for custom
model code. The existing host route and cancellation contract must remain the
only entry point.

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
show the chosen local runtime and reason. Unapproved remote endpoints must be
rejected before any request is sent; an approved OpenAI cloud route must never
participate in local AirLLM fallback.

**Closure check.** Tests cover normal success, known capacity failure, unrelated
failure, unsupported model ID, missing runtime, cancellation, and fallback
failure. Each outcome records provider, model, reason, and whether a second
attempt was made.

### Corrected in this branch: host lifecycle and long-inference control

**Observation.** Electron now supervises a bounded host deadline, resolves
pending requests on timeout, kills the Windows process tree, and starts a fresh
host. `chat.complete` passes the same deadline cancellation into the local
OpenAI-compatible and AirLLM paths. The smoke test launches the direct host
executable and asserts clean exit.

**Condition.** A long local/AirLLM request is active, the window closes, or a
smoke test ends while the `dotnet run` child and actual host process have
different lifetimes.

**Consequence.** The original orphan-process and uncancellable-request risk is
substantially reduced. Release evidence still needs a real Electron AirLLM
cancel/timeout journey, but the host-level safeguards are now in place.

**Closure evidence.** Keep the direct executable launch, process-tree shutdown,
bounded pending requests, and deadline propagation covered by the runtime audit
and host smoke tests.

**Remaining closure check.** Exercise the same guarantees through the packaged
Electron AirLLM path and verify no host process remains after cancellation and
window close.

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
identity, the catalog uses local-intent wording, and the host returns provider
identity with settings and discovered models. The replacement shell still does
not provide the complete setup flow or an in-context two-choice picker.

**Correction.** Add a small intent-based setup flow after the host contract is
stable: local models, very large local models, cloud models, cloud voice, cloud
images, and other supported services. Each choice maps to a typed provider
profile with health, capabilities, privacy, auth state, install state, and a
clear next action. Never expose an arbitrary unvalidated endpoint as a generic
shortcut.

## Agent decision audit

### `Add future features list`

The agent's broad Chat/coding redesign was not a healthy next step: it ignored
the parent scope, created renderer drift and capture residue, and committed a
large UI slice while the provider boundary was still being audited. That commit
was explicitly reverted. The chat is now archived and interrupted; it must not
resume without a narrow, parent-assigned UI task.

### `Find a lightweight TTS model`

The decision to consolidate voice into the existing C# host is healthy and
matches the owner decision. The earlier Rust sidecar was a wrong turn for this
repository. Its lifecycle checkpoint is accepted: deadlines, cancellation,
process-tree cleanup, and direct-host smoke evidence passed. Kokoro/Pocket TTS
remain future C# adapters, not promises that depend on a removed Rust runtime.

### Parent/orchestrator decisions

The original push preserved user work and protected `main`, which was correct.
The mistake was treating the whole active worktree as one releasable commit.
The current correction is to keep the branch, avoid destructive rollback, and
use narrow follow-up commits plus architecture gates before any merge.

## Priority correction sequence

1. Keep both implementation chats paused; the UI chat's broad Chat/coding
   refactor was rejected and reverted because it crossed its assigned scope.
2. Keep generated artifacts and all Rust voice residue out; retain the now
   deterministic host lifecycle and local-first transport boundary.
3. Complete AirLLM provenance, model compatibility, trust/consent, disk/runtime
   health, and bounded fallback tests.
4. Add the intent-based provider setup flow in Electron on top of durable
   provider identities for local/AirLLM, cloud models, cloud voice, and other
   supported services.
5. Implement OS-backed credentials and service-specific OAuth/API-key flows;
   never pass secrets through ordinary settings or logs.
6. Re-run packaged Electron acceptance and a real disposable AirLLM model test
   before discussing merge.

## Open evidence gaps

- No Electron end-to-end AirLLM request has been demonstrated.
- No packaged installer contains or provisions the AirLLM runtime.
- No resource/disk estimate or cleanup policy has been verified for layer shards.
- No safe policy has been demonstrated for AirLLM remote/custom model code.
- No packaged Electron AirLLM cancellation/timeout/close-window test has passed.
- The full Electron local-intent setup flow is not yet implemented.
- No cloud provider credential store or explicit cloud-route acceptance exists.
- No service-specific OAuth/API-key flows exist yet; account sign-in and API-key
  auth must not be conflated.
- No release review has separated the large mixed branch snapshot into focused
  acceptance units.

## Acceptance bar before merge

The feature is not “done” when a Python worker starts. It is ready for review
only when the approved Electron path can select and explain a local AirLLM
attempt or an explicitly authorized OpenAI route, preserve the selected data
boundary, cancel it, recover from failure, and leave no orphan process or
ambiguous provider state. The user must be able to tell what Skai is doing, why
it chose that runtime or account, what it downloaded or billed, and how to stop
or disconnect the choice.
