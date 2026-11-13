# Security, privacy and trust boundaries

## Attack surfaces
User prompt, model-generated tool calls, malicious repo files/AGENTS guidance, untrusted skill manifests, MCP servers, GitHub search issues/PRs, network model endpoints, archive attachments, local files/symlinks, terminal commands, release/update feeds and injected UI content. Treat all as untrusted input, not authority.

## Permission model
Operation capability = `(principal, workspace, tool, action, resource path/host, risk, expiry, approvalId)`; enforce in backend immediately before effect. Minimal grants by default; per-operation approvals for shell, filesystem writes outside declared scope, Git push, package install, remote egress or privileged operations. Persist grant origin and revoke capability. Skills/MCP tools never mint grants for themselves.

## Approval UX
Show exact operation, provenance (which agent/skill), execution root, commands/files/hosts affected, whether request may send data externally, anticipated side effects and user choices. Allow/deny once; optional durable policy requires separate explicit confirmation and clear scope. Approval rejected/expired should be a normal run state, not crash.

## Execution hardening
Commands run with user's privileges unless an actual sandbox exists; label the limit. Never call un-sandboxed execution 'sandboxed'. For hardened modes evaluate OS-specific isolation (Windows restricted token/appcontainer/job policy where feasible; Linux namespaces/bubblewrap/seccomp where feasible), availability tests, filesystem/network controls and explicit unsupported-platform state. A permission prompt is NOT a sandbox. No blanket bypass option or prompt-only injection defense.

## Data egress
Local profile must not send prompts/context to cloud endpoints, hosted embeddings/search or telemetry without informed consent. Before remote request, preview provider/host and context categories; inspect attachments/repo files. Endpoints have allowlist/scheme/SSRF checks, especially when remote-configurable MCP or ComfyUI. Loopback-only model detection by default.

## Credentials
OS credential manager/Secret Service/keyring with clear setup. Never print tokens, put in args, environment inherited by child, IPC payload logs or diagnostics archive. On Linux without supported secure store disable persistent secret save and offer supported setup; do not fall back to plaintext silently. Revoke/remove credential references cleanly.

## Skill supply chain
No `curl | sh` installs, unchecked postinstall, unlimited Git clones or default auto-execution. GitHub URI and commit pin, license, hash, signature if provided, permission/dependency diff and ownership. Stage into temp dir, validate, then atomic activation. Update revokes/reasks changed permissions. Remove only owned content, preserve data and shared deps. Odysseus is ideas only.

## Threat-case tests
Prompt injection in README asks to upload secrets; model outputs a tool call outside workspace; symlink to home SSH; MCP tool spoofs privilege; repo install script mutates system; skill update requests new network host; cloud fallback under Local; untrusted web snippet says bypass policy; terminal output includes token; updater points to attacker domain; archive contains zip-slip; file dialog opens below another modal. All must fail safely with reviewable evidence.
